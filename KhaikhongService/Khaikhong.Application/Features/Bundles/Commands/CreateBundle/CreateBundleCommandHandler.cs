using System;
using System.Collections.Generic;
using System.Linq;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandHandler(
    IBundleRepository bundleRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateBundleCommandHandler> logger) : IRequestHandler<CreateBundleCommand, ApiResponse<CreateBundleResponseDto>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CreateBundleCommandHandler> _logger = logger;

    public async Task<ApiResponse<CreateBundleResponseDto>> Handle(CreateBundleCommand request, CancellationToken cancellationToken)
    {
        CreateBundleRequestDto payload = request.Request;

        if (payload.Products is null || payload.Products.Count == 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "request.products", error = "At least one product must be included in the bundle." }
                });
        }

        HashSet<Guid> productIds = payload.Products
            .Select(product => product.ProductId)
            .ToHashSet();

        if (productIds.Count != payload.Products.Count)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "request.products", error = "Duplicate product ids are not allowed." }
                });
        }

        bool allProductsActive = await _productRepository.AreProductsActiveAsync(productIds, cancellationToken);
        if (!allProductsActive)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "request.products", error = "One or more products are inactive or do not exist." }
                });
        }

        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> variantLookup =
            await _productRepository.GetActiveVariantsForProductsAsync(productIds, cancellationToken);

        List<object> quantityErrors = CollectQuantityErrors(payload.Products);
        if (quantityErrors.Count > 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: quantityErrors.ToArray());
        }

        List<object> variantErrors = CollectVariantErrors(payload.Products, variantLookup);
        if (variantErrors.Count > 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: variantErrors.ToArray());
        }

        Bundle bundle = Bundle.Create(payload.Name, payload.Price, payload.Description);

        IList<BundleItem> items = BuildBundleItems(bundle.Id, payload.Products);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Creating bundle {BundleName} with {ItemCount} items", bundle.Name, items.Count);

            await _bundleRepository.AddAsync(bundle, cancellationToken);
            await _unitOfWork.CompleteAsync();

            await _bundleRepository.BulkInsertItemsAsync(items, cancellationToken);
            await _unitOfWork.CompleteAsync();

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Bundle {BundleId} created successfully", bundle.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bundle {BundleName}", bundle.Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        bundle.AddItems(items);

        CreateBundleResponseDto response = new()
        {
            Id = bundle.Id,
            Name = bundle.Name,
            Description = bundle.Description,
            Price = bundle.Price,
            ProductCount = items.Count,
            Items = items
                .Select(item => new BundleItemResponseDto
                {
                    ProductId = item.ProductId ?? throw new InvalidOperationException("Bundle item must reference a product."),
                    VariantId = item.VariantId,
                    Quantity = item.Quantity
                })
                .ToList()
        };

        return ApiResponse<CreateBundleResponseDto>.Success(
            status: 200,
            message: "Bundle created successfully",
            data: response);
    }

    private static IList<BundleItem> BuildBundleItems(
        Guid bundleId,
        IReadOnlyCollection<CreateBundleProductDto> products)
    {
        List<BundleItem> items = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is not null && dto.Variants.Count > 0)
            {
                foreach (CreateBundleVariantDto variant in dto.Variants)
                {
                    items.Add(BundleItem.Create(bundleId, variant.Quantity, dto.ProductId, variant.VariantId));
                }

                continue;
            }

            items.Add(BundleItem.Create(bundleId, dto.Quantity!.Value, dto.ProductId));
        }

        return items;
    }

    private static List<object> CollectVariantErrors(
        IReadOnlyCollection<CreateBundleProductDto> products,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> variantLookup)
    {
        List<object> errors = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                continue;
            }

            if (!variantLookup.TryGetValue(dto.ProductId, out IReadOnlyCollection<Guid>? validVariants))
            {
                foreach (CreateBundleVariantDto variant in dto.Variants)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} is not active for product {dto.ProductId}."
                    });
                }

                continue;
            }

            HashSet<Guid> variantSet = validVariants is HashSet<Guid> hash
                ? hash
                : new HashSet<Guid>(validVariants);

            foreach (CreateBundleVariantDto variant in dto.Variants)
            {
                if (!variantSet.Contains(variant.VariantId))
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} is not active for product {dto.ProductId}."
                    });
                }
            }
        }

        return errors;
    }

    private static List<object> CollectQuantityErrors(IReadOnlyCollection<CreateBundleProductDto> products)
    {
        List<object> errors = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                if (!dto.Quantity.HasValue || dto.Quantity.Value <= 0)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Quantity must be greater than zero for product {dto.ProductId} when no variants are specified."
                    });
                }

                continue;
            }

            foreach (CreateBundleVariantDto variant in dto.Variants)
            {
                if (variant.Quantity <= 0)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variant.VariantId} must have a quantity greater than zero for product {dto.ProductId}."
                    });
                }
            }
        }

        return errors;
    }
}

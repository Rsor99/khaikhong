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

        List<object> variantErrors = CollectVariantErrors(payload.Products, variantLookup);
        if (variantErrors.Count > 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: variantErrors.ToArray());
        }

        Bundle bundle = Bundle.Create(payload.Name, payload.Price, payload.Description);

        IList<BundleItem> items = BuildBundleItems(bundle.Id, payload.Products, variantLookup);

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
        IReadOnlyCollection<BundleProductDto> products,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> variantLookup)
    {
        List<BundleItem> items = new();

        foreach (BundleProductDto dto in products)
        {
            IReadOnlyCollection<Guid> validVariants = variantLookup.TryGetValue(dto.ProductId, out IReadOnlyCollection<Guid>? variants)
                ? variants
                : Array.Empty<Guid>();

            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                items.Add(BundleItem.Create(bundleId, dto.Quantity, productId: dto.ProductId));
                continue;
            }

            foreach (Guid variantId in dto.Variants)
            {
                items.Add(BundleItem.Create(bundleId, dto.Quantity, dto.ProductId, variantId));
            }
        }

        return items;
    }

    private static List<object> CollectVariantErrors(
        IReadOnlyCollection<BundleProductDto> products,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> variantLookup)
    {
        List<object> errors = new();

        foreach (BundleProductDto dto in products)
        {
            if (dto.Variants is null || dto.Variants.Count == 0)
            {
                continue;
            }

            if (!variantLookup.TryGetValue(dto.ProductId, out IReadOnlyCollection<Guid>? validVariants))
            {
                foreach (Guid variantId in dto.Variants)
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variantId} is not active for product {dto.ProductId}."
                    });
                }

                continue;
            }

            HashSet<Guid> variantSet = validVariants is HashSet<Guid> hash
                ? hash
                : new HashSet<Guid>(validVariants);

            foreach (Guid variantId in dto.Variants)
            {
                if (!variantSet.Contains(variantId))
                {
                    errors.Add(new
                    {
                        field = "request.products",
                        error = $"Variant {variantId} is not active for product {dto.ProductId}."
                    });
                }
            }
        }

        return errors;
    }
}

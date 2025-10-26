using System;
using System.Collections.Generic;
using System.Linq;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandHandler(
    IBundleRepository bundleRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateBundleCommandHandler> logger) : IRequestHandler<CreateBundleCommand, ApiResponse<CreateBundleResponseDto>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;
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

        List<object> quantityErrors = BundleCommandHelper.CollectQuantityErrors(payload.Products);
        if (quantityErrors.Count > 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: quantityErrors.ToArray());
        }

        List<object> variantErrors = BundleCommandHelper.CollectVariantErrors(payload.Products, variantLookup);
        if (variantErrors.Count > 0)
        {
            return ApiResponse<CreateBundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: variantErrors.ToArray());
        }

        Bundle bundle = Bundle.Create(payload.Name, payload.Price, payload.Description);

        Guid? currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            bundle.SetCreatedBy(currentUserId.Value);
        }

        IList<BundleItem> items = BundleCommandHelper.BuildBundleItems(bundle.Id, payload.Products);

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
}

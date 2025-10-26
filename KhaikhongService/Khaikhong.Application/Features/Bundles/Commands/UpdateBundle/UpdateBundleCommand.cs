using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Queries;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Bundles.Commands.UpdateBundle;

public sealed record UpdateBundleCommand(Guid BundleId, CreateBundleRequestDto Request) : IRequest<ApiResponse<BundleResponseDto>>;

public sealed class UpdateBundleCommandHandler(
    IBundleRepository bundleRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<UpdateBundleCommandHandler> logger) : IRequestHandler<UpdateBundleCommand, ApiResponse<BundleResponseDto>>
{
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<UpdateBundleCommandHandler> _logger = logger;

    public async Task<ApiResponse<BundleResponseDto>> Handle(UpdateBundleCommand request, CancellationToken cancellationToken)
    {
        CreateBundleRequestDto payload = request.Request;

        if (payload.Products is null || payload.Products.Count == 0)
        {
            return ApiResponse<BundleResponseDto>.Fail(
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
            return ApiResponse<BundleResponseDto>.Fail(
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
            return ApiResponse<BundleResponseDto>.Fail(
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
            return ApiResponse<BundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: quantityErrors.ToArray());
        }

        List<object> variantErrors = BundleCommandHelper.CollectVariantErrors(payload.Products, variantLookup);
        if (variantErrors.Count > 0)
        {
            return ApiResponse<BundleResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: variantErrors.ToArray());
        }

        Bundle? bundle = await _bundleRepository.GetDetailedByIdTrackingAsync(request.BundleId, cancellationToken);
        if (bundle is null)
        {
            _logger.LogWarning("Bundle {BundleId} not found for update", request.BundleId);
            return ApiResponse<BundleResponseDto>.Fail(
                status: 404,
                message: "Bundle not found");
        }

        bundle.UpdateDetails(payload.Name, payload.Price, payload.Description);

        Guid? currentUserId = _currentUserService.UserId;
        if (currentUserId.HasValue)
        {
            bundle.SetUpdatedBy(currentUserId.Value);
        }

        Dictionary<(Guid ProductId, Guid? VariantId), BundleItem> existingItems = BuildExistingItemLookup(bundle.Items);
        Dictionary<(Guid ProductId, Guid? VariantId), int> requestedItems = BuildRequestedItemLookup(payload.Products);

        foreach (((Guid ProductId, Guid? VariantId) key, int quantity) in requestedItems)
        {
            if (existingItems.TryGetValue(key, out BundleItem? existingItem))
            {
                if (!existingItem.IsActive)
                {
                    existingItem.Activate();
                }

                if (existingItem.Quantity != quantity)
                {
                    existingItem.UpdateQuantity(quantity);
                }

                continue;
            }

            BundleItem newItem = key.VariantId.HasValue
                ? BundleItem.Create(bundle.Id, quantity, key.ProductId, key.VariantId.Value)
                : BundleItem.Create(bundle.Id, quantity, key.ProductId);

            bundle.AddItems(new[] { newItem });
            existingItems[key] = newItem;
        }

        foreach (KeyValuePair<(Guid ProductId, Guid? VariantId), BundleItem> pair in existingItems)
        {
            if (!requestedItems.ContainsKey(pair.Key) && pair.Value.IsActive)
            {
                pair.Value.Deactivate();
            }
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update bundle {BundleId}", request.BundleId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        Bundle? refreshedBundle = await _bundleRepository.GetDetailedByIdAsync(bundle.Id, cancellationToken);

        BundleResponseDto response = BundleResponseMapper.Map(refreshedBundle ?? bundle);

        _logger.LogInformation("Bundle {BundleId} updated successfully", bundle.Id);

        return ApiResponse<BundleResponseDto>.Success(
            status: 200,
            message: "Bundle updated successfully",
            data: response);
    }

    private static Dictionary<(Guid ProductId, Guid? VariantId), BundleItem> BuildExistingItemLookup(IEnumerable<BundleItem> items)
    {
        Dictionary<(Guid ProductId, Guid? VariantId), BundleItem> lookup = new();

        foreach (BundleItem item in items)
        {
            if (!item.ProductId.HasValue)
            {
                continue;
            }

            (Guid ProductId, Guid? VariantId) key = (item.ProductId.Value, item.VariantId);

            if (!lookup.TryGetValue(key, out BundleItem? existing) || (!existing.IsActive && item.IsActive))
            {
                lookup[key] = item;
            }
        }

        return lookup;
    }

    private static Dictionary<(Guid ProductId, Guid? VariantId), int> BuildRequestedItemLookup(IReadOnlyCollection<CreateBundleProductDto> products)
    {
        Dictionary<(Guid ProductId, Guid? VariantId), int> lookup = new();

        foreach (CreateBundleProductDto dto in products)
        {
            if (dto.Variants is not null && dto.Variants.Count > 0)
            {
                foreach (CreateBundleVariantDto variant in dto.Variants)
                {
                    (Guid, Guid?) key = (dto.ProductId, variant.VariantId);
                    lookup[key] = variant.Quantity;
                }

                continue;
            }

            (Guid, Guid?) productKey = (dto.ProductId, null);
            lookup[productKey] = dto.Quantity!.Value;
        }

        return lookup;
    }
}

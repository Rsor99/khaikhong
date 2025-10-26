using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Orders.Commands.CreateOrder;

public sealed record CreateOrderCommand(CreateOrderRequestDto Request) : IRequest<ApiResponse<CreateOrderResponseDto>>;

public sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IVariantRepository variantRepository,
    IBundleRepository bundleRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    ILogger<CreateOrderCommandHandler> logger) : IRequestHandler<CreateOrderCommand, ApiResponse<CreateOrderResponseDto>>
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "product", "variant", "bundle"
    };

    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IBundleRepository _bundleRepository = bundleRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<CreateOrderCommandHandler> _logger = logger;

    private readonly Dictionary<Guid, Product> _productCache = new();
    private readonly Dictionary<Guid, Variant> _variantCache = new();

    public async Task<ApiResponse<CreateOrderResponseDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        CreateOrderRequestDto payload = request.Request;

        if (payload.Items is null || payload.Items.Count == 0)
        {
            return ApiResponse<CreateOrderResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[] { new { field = "request.items", error = "At least one item must be included." } });
        }

        Guid? currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Order creation requested without authenticated user");
            return ApiResponse<CreateOrderResponseDto>.Fail(
                status: 401,
                message: "Unauthorized");
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        Order order = Order.Create(currentUserId.Value);
        await _orderRepository.AddAsync(order);

        foreach (CreateOrderItemRequestDto item in payload.Items)
        {
            if (item.Quantity <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ApiResponse<CreateOrderResponseDto>.Fail(
                    status: 400,
                    message: "Validation failed",
                    errors: new[] { new { field = "request.items", error = "Quantity must be greater than zero." } });
            }

            if (!SupportedTypes.Contains(item.Type))
            {
                await transaction.RollbackAsync(cancellationToken);
                return ApiResponse<CreateOrderResponseDto>.Fail(
                    status: 400,
                    message: "Validation failed",
                    errors: new[] { new { field = "request.items", error = $"Unsupported item type '{item.Type}'." } });
            }

            switch (item.Type.ToLowerInvariant())
            {
                case "product":
                    {
                        Product? product = await GetProductAsync(item.Id, cancellationToken);
                        if (product is null || !product.IsActive)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return ApiResponse<CreateOrderResponseDto>.Fail(404, "Product not found");
                        }

                        if (!product.BaseStock.HasValue || product.BaseStock.Value < item.Quantity)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return ApiResponse<CreateOrderResponseDto>.Fail(400, "Insufficient stock");
                        }

                        product.SetBaseStock(product.BaseStock.Value - item.Quantity);
                        _productRepository.Update(product);

                        order.AddItem(OrderItem.Create(order.Id, item.Quantity, productId: product.Id));
                        break;
                    }

                case "variant":
                    {
                        Variant? variant = await GetVariantAsync(item.Id, cancellationToken);
                        if (variant is null || !variant.IsActive)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return ApiResponse<CreateOrderResponseDto>.Fail(404, "Variant not found");
                        }

                        if (variant.Stock < item.Quantity)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return ApiResponse<CreateOrderResponseDto>.Fail(400, "Insufficient stock");
                        }

                        variant.UpdateInventory(variant.Stock - item.Quantity);
                        _variantRepository.Update(variant);

                        order.AddItem(OrderItem.Create(order.Id, item.Quantity, variantId: variant.Id));
                        break;
                    }

                case "bundle":
                    {
                        Bundle? bundle = await _bundleRepository.GetDetailedByIdTrackingAsync(item.Id, cancellationToken);
                        if (bundle is null || !bundle.IsActive)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            return ApiResponse<CreateOrderResponseDto>.Fail(404, "Bundle not found");
                        }

                        List<(Product? Product, Variant? Variant, int Quantity)> adjustments = new();

                        foreach (BundleItem bundleItem in bundle.Items.Where(b => b.IsActive))
                        {
                            int requiredQuantity = bundleItem.Quantity * item.Quantity;

                            if (bundleItem.VariantId.HasValue)
                            {
                                Variant? variant = bundleItem.Variant ?? await GetVariantAsync(bundleItem.VariantId.Value, cancellationToken);
                                if (variant is null || !variant.IsActive || variant.Stock < requiredQuantity)
                                {
                                    await transaction.RollbackAsync(cancellationToken);
                                    return ApiResponse<CreateOrderResponseDto>.Fail(400, "Insufficient stock");
                                }

                                _variantCache[variant.Id] = variant;
                                adjustments.Add((null, variant, requiredQuantity));
                            }
                            else if (bundleItem.ProductId.HasValue)
                            {
                                Product? product = bundleItem.Product ?? await GetProductAsync(bundleItem.ProductId.Value, cancellationToken);
                                if (product is null || !product.IsActive || !product.BaseStock.HasValue || product.BaseStock.Value < requiredQuantity)
                                {
                                    await transaction.RollbackAsync(cancellationToken);
                                    return ApiResponse<CreateOrderResponseDto>.Fail(400, "Insufficient stock");
                                }

                                _productCache[product.Id] = product;
                                adjustments.Add((product, null, requiredQuantity));
                            }
                            else
                            {
                                await transaction.RollbackAsync(cancellationToken);
                                return ApiResponse<CreateOrderResponseDto>.Fail(400, "Bundle contains invalid items");
                            }
                        }

                        foreach ((Product? product, Variant? variant, int quantity) in adjustments)
                        {
                            if (product is not null)
                            {
                                product.SetBaseStock(product.BaseStock!.Value - quantity);
                                _productRepository.Update(product);
                            }

                            if (variant is not null)
                            {
                                variant.UpdateInventory(variant.Stock - quantity);
                                _variantRepository.Update(variant);
                            }
                        }

                        order.AddItem(OrderItem.Create(order.Id, item.Quantity, bundleId: bundle.Id));
                        break;
                    }
            }
        }

        try
        {
            await _unitOfWork.CompleteAsync();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create order for user {UserId}", currentUserId.Value);
            throw;
        }

        int totalQuantity = order.Items.Sum(item => item.Quantity);

        CreateOrderResponseDto response = new()
        {
            OrderId = order.Id,
            ItemCount = totalQuantity
        };

        _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, currentUserId.Value);

        return ApiResponse<CreateOrderResponseDto>.Success(
            status: 200,
            message: "Order created successfully",
            data: response);
    }

    private async Task<Product?> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (_productCache.TryGetValue(productId, out Product? product))
        {
            return product;
        }

        product = await _productRepository.GetDetailedByIdTrackingAsync(productId, cancellationToken);
        if (product is not null)
        {
            _productCache[productId] = product;
        }

        return product;
    }

    private async Task<Variant?> GetVariantAsync(Guid variantId, CancellationToken cancellationToken)
    {
        if (_variantCache.TryGetValue(variantId, out Variant? variant))
        {
            return variant;
        }

        variant = await _variantRepository.GetByIdTrackingAsync(variantId, cancellationToken);
        if (variant is not null)
        {
            _variantCache[variantId] = variant;
        }

        return variant;
    }
}

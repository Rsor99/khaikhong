using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Orders.Commands.CreateOrder;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IVariantRepository> _variantRepository = new();
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<CreateOrderCommandHandler>> _logger = new();

    public CreateOrderCommandHandlerTests()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _unitOfWork.Setup(unit => unit.CompleteAsync())
            .ReturnsAsync(1);

        _orderRepository
            .Setup(repo => repo.AddAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenProductStockSufficient()
    {
        Guid userId = Guid.Parse("019a4000-aaaa-bbbb-cccc-ffffffff0001");
        Guid productId = Guid.Parse("019a4000-aaaa-bbbb-cccc-ffffffff0002");

        Product product = Product.Create("Reusable Bottle", 100m, null, null, 5);
        SetId(product, productId);

        _productRepository
            .Setup(repo => repo.GetDetailedByIdTrackingAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        CreateOrderRequestDto request = new()
        {
            Items = new List<CreateOrderItemRequestDto>
            {
                new()
                {
                    Id = productId,
                    Type = "product",
                    Quantity = 2
                }
            }
        };

        CreateOrderCommandHandler handler = BuildHandler();

        ApiResponse<CreateOrderResponseDto> response = await handler.Handle(new CreateOrderCommand(request), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Data);
        Assert.Equal(3, product.BaseStock);
        _orderRepository.Verify(repo => repo.AddAsync(It.IsAny<Order>()), Times.Once);
        _productRepository.Verify(repo => repo.Update(product), Times.Once);
        _unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenVariantStockInsufficient()
    {
        Guid userId = Guid.Parse("019a4000-aaaa-bbbb-cccc-ffffffff0003");
        Guid variantId = Guid.Parse("019a4000-aaaa-bbbb-cccc-ffffffff0004");

        Variant variant = Variant.Create(Guid.NewGuid(), 150m, 1, "VAR-001");
        SetId(variant, variantId);

        _variantRepository
            .Setup(repo => repo.GetByIdTrackingAsync(variantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(variant);

        _currentUserService
            .Setup(service => service.UserId)
            .Returns(userId);

        CreateOrderRequestDto request = new()
        {
            Items = new List<CreateOrderItemRequestDto>
            {
                new()
                {
                    Id = variantId,
                    Type = "variant",
                    Quantity = 2
                }
            }
        };

        CreateOrderCommandHandler handler = BuildHandler();

        ApiResponse<CreateOrderResponseDto> response = await handler.Handle(new CreateOrderCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Never);
    }

    private CreateOrderCommandHandler BuildHandler() => new(
        _orderRepository.Object,
        _productRepository.Object,
        _variantRepository.Object,
        _bundleRepository.Object,
        _unitOfWork.Object,
        _currentUserService.Object,
        _logger.Object);

    private static void SetId(object entity, Guid id)
    {
        FieldInfo? idField = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        idField?.SetValue(entity, id);
    }
}

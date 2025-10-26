using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Products.Commands.DeleteProduct;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<DeleteProductCommandHandler>> _logger = new();

    public DeleteProductCommandHandlerTests()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        Guid productId = Guid.NewGuid();
        _productRepository
            .Setup(repo => repo.GetDetailedByIdTrackingAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        DeleteProductCommandHandler handler = BuildHandler();

        ApiResponse<object> response = await handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
        _productRepository.Verify(repo => repo.GetDetailedByIdTrackingAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSoftDelete_WhenProductExists()
    {
        Product product = BuildProduct();
        Guid productId = product.Id;

        _productRepository
            .Setup(repo => repo.GetDetailedByIdTrackingAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        DeleteProductCommandHandler handler = BuildHandler();

        ApiResponse<object> response = await handler.Handle(new DeleteProductCommand(productId), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.False(product.IsActive);
        Assert.All(product.Options, option => Assert.False(option.IsActive));
        Assert.All(product.Variants, variant => Assert.False(variant.IsActive));

        _unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Once);
    }

    private DeleteProductCommandHandler BuildHandler() => new(
        _productRepository.Object,
        _unitOfWork.Object,
        _logger.Object);

    private static Product BuildProduct()
    {
        Product product = Product.Create("Premium Hoodie", 79.99m, "Cozy fleece hoodie", "HD-001", 120);

        VariantOption option = VariantOption.Create(product.Id, "Color");
        VariantOptionValue value = VariantOptionValue.Create(option.Id, "Black");
        option.AddValues(new[] { value });
        product.AddOptions(new[] { option });

        Variant variant = Variant.Create(product.Id, 89.99m, 25, "HD-001-BLK-S");
        ProductVariantCombination combination = ProductVariantCombination.Create(variant.Id, value.Id);
        combination.AttachOptionValue(value);
        variant.AddCombinations(new[] { combination });
        product.AddVariants(new[] { variant });

        value.AddCombinations(new[] { combination });

        return product;
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Bundles.Commands.CreateBundle;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Khaikhong.Application.Contracts.Services;

namespace Khaikhong.Tests.Features.Bundles.Commands.CreateBundle;

public sealed class CreateBundleCommandHandlerTests
{
    private readonly Mock<IBundleRepository> _bundleRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<ILogger<CreateBundleCommandHandler>> _logger = new();

    public CreateBundleCommandHandlerTests()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        _unitOfWork.Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenNoProducts()
    {
        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = Array.Empty<CreateBundleProductDto>()
        };

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _productRepository.Verify(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenProductsInactive()
    {
        CreateBundleRequestDto request = BuildValidRequest();

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>());

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _bundleRepository.Verify(repo => repo.AddAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenVariantInactive()
    {
        Guid productId = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873");
        Guid variantId = Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7141111");

        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(productId, new List<CreateBundleVariantDto>
                {
                    new(variantId, 1)
                }, null)
            }
        };

        Guid currentUserId = Guid.Parse("019a1f40-dead-beef-b429-d4def7149999");
        _currentUserService
            .Setup(service => service.UserId)
            .Returns(currentUserId);

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>());

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
    }

    [Fact]
    public async Task Handle_ShouldCreateBundle_WhenValid()
    {
        Guid productId1 = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873");
        Guid productId2 = Guid.Parse("019a1cb4-f2db-7559-bf61-5d23ee22516e");
        Guid variantId = Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7142222");

        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(productId1, null, 1),
                new(productId2, new List<CreateBundleVariantDto>
                {
                    new(variantId, 2)
                }, null)
            }
        };

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>());

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>
            {
                [productId2] = new List<Guid> { variantId }
            });

        Guid currentUserId = Guid.Parse("019a1f40-dead-beef-b429-d4def7149999");
        _currentUserService
            .Setup(service => service.UserId)
            .Returns(currentUserId);

        _bundleRepository
            .Setup(repo => repo.AddAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _bundleRepository
            .Setup(repo => repo.BulkInsertItemsAsync(It.IsAny<IEnumerable<BundleItem>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.Equal(2, response.Data!.ProductCount);
        Assert.Equal(2, response.Data.Items.Count);
        Assert.Collection(response.Data.Items,
            item =>
            {
                Assert.Equal(productId1, item.ProductId);
                Assert.Null(item.VariantId);
                Assert.Equal(1, item.Quantity);
            },
            item =>
            {
                Assert.Equal(productId2, item.ProductId);
                Assert.Equal(variantId, item.VariantId);
                Assert.Equal(2, item.Quantity);
            });

        _bundleRepository.Verify(repo => repo.AddAsync(It.Is<Bundle>(bundle =>
            bundle.CreatedBy == currentUserId && bundle.UpdatedBy == currentUserId), It.IsAny<CancellationToken>()), Times.Once);
        _bundleRepository.Verify(repo => repo.BulkInsertItemsAsync(It.IsAny<IEnumerable<BundleItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unit => unit.CompleteAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenQuantityMissingForNonVariantProduct()
    {
        Guid productId = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873");

        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(productId, null, null)
            }
        };

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>());

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _bundleRepository.Verify(repo => repo.AddAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenVariantQuantityInvalid()
    {
        Guid productId = Guid.Parse("019a1cb4-f2db-7559-bf61-5d23ee22516e");
        Guid variantId = Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7142222");

        CreateBundleRequestDto request = new()
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(productId, new List<CreateBundleVariantDto>
                {
                    new(variantId, 0)
                }, null)
            }
        };

        _productRepository
            .Setup(repo => repo.AreProductsActiveAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _productRepository
            .Setup(repo => repo.GetActiveVariantsForProductsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyCollection<Guid>>
            {
                [productId] = new List<Guid> { variantId }
            });

        CreateBundleCommandHandler handler = BuildHandler();

        ApiResponse<CreateBundleResponseDto> response = await handler.Handle(new CreateBundleCommand(request), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        _bundleRepository.Verify(repo => repo.AddAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private CreateBundleCommandHandler BuildHandler() => new(
        _bundleRepository.Object,
        _productRepository.Object,
        _unitOfWork.Object,
        _currentUserService.Object,
        _logger.Object);

    private static CreateBundleRequestDto BuildValidRequest()
    {
        Guid productId1 = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873");
        Guid productId2 = Guid.Parse("019a1cb4-f2db-7559-bf61-5d23ee22516e");
        Guid variantId = Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7142222");

        return new CreateBundleRequestDto
        {
            Name = "Eco Starter Kit",
            Description = "A beginner-friendly sustainability bundle",
            Price = 1290m,
            Products = new List<CreateBundleProductDto>
            {
                new(productId1, null, 1),
                new(productId2, new List<CreateBundleVariantDto>
                {
                    new(variantId, 2)
                }, null)
            }
        };
    }
}

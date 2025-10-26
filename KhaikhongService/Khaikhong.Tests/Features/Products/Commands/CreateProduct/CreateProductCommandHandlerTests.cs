using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Products.Commands.CreateProduct;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CreateProductCommandHandler>> _loggerMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        Mock<IUserRepository> userRepositoryMock = new();

        _unitOfWorkMock.SetupGet(unit => unit.Users).Returns(userRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(unit => unit.Products).Returns(_productRepositoryMock.Object);
        _unitOfWorkMock.Setup(unit => unit.CompleteAsync()).ReturnsAsync(1);

        _mapperMock
            .Setup(mapper => mapper.Map<Product>(It.IsAny<CreateProductRequestDto>()))
            .Returns((CreateProductRequestDto dto) => Product.Create(dto.Name, dto.BasePrice, dto.Description, dto.Sku));

        _mapperMock
            .Setup(mapper => mapper.Map<CreateProductResponseDto>(It.IsAny<Product>()))
            .Returns((Product product) => new CreateProductResponseDto
            {
                Id = product.Id,
                BasePrice = product.BasePrice
            });

        Guid currentUserId = Guid.Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb");

        _currentUserServiceMock
            .Setup(service => service.UserId)
            .Returns(currentUserId);

        _handler = new CreateProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _currentUserServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenNameExists()
    {
        CreateProductCommand command = new(CreateValidRequest() with { Name = "Existing Product" });

        _productRepositoryMock
            .Setup(repository => repository.ExistsByNameOrSkuAsync(command.Request.Name, command.Request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, false));

        ApiResponse<CreateProductResponseDto> response = await _handler.Handle(command, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        Assert.Equal("Validation failed", response.Message);
        _productRepositoryMock.Verify(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenSkuExists()
    {
        CreateProductCommand command = new(CreateValidRequest() with { Sku = "SKU-001" });

        _productRepositoryMock
            .Setup(repository => repository.ExistsByNameOrSkuAsync(command.Request.Name, command.Request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, true));

        ApiResponse<CreateProductResponseDto> response = await _handler.Handle(command, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        Assert.Equal("Validation failed", response.Message);
        _productRepositoryMock.Verify(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationFailure_WhenSelectionIsMissing()
    {
        CreateProductRequestDto request = CreateValidRequest() with
        {
            Variants = new[]
            {
                new ProductVariantDto
                {
                    Sku = "SKU-501",
                    Price = 199.99m,
                    Stock = 5,
                    Selections = new[]
                    {
                        new VariantSelectionDto { OptionName = "Color", Value = "Black" },
                        new VariantSelectionDto { OptionName = "Size", Value = "XL" }
                    }
                }
            }
        };

        CreateProductCommand command = new(request);

        _productRepositoryMock
            .Setup(repository => repository.ExistsByNameOrSkuAsync(command.Request.Name, command.Request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, false));

        ApiResponse<CreateProductResponseDto> response = await _handler.Handle(command, CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(400, response.Status);
        Assert.Equal("Validation failed", response.Message);
        _productRepositoryMock.Verify(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductSuccessfully()
    {
        CreateProductCommand command = new(CreateValidRequest());

        _productRepositoryMock
            .Setup(repository => repository.ExistsByNameOrSkuAsync(command.Request.Name, command.Request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, false));

        Mock<IUnitOfWorkTransaction> transactionMock = CreateTransactionMock();

        _unitOfWorkMock
            .Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _productRepositoryMock
            .Setup(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ApiResponse<CreateProductResponseDto> response = await _handler.Handle(command, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.Equal("Product created successfully", response.Message);
        Assert.NotNull(response.Data);

        _productRepositoryMock.Verify(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(unit => unit.CompleteAsync(), Times.Once);
        transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateProductSuccessfully_WhenVariantsMissing()
    {
        CreateProductRequestDto request = CreateValidRequest() with
        {
            Variants = null!
        };

        CreateProductCommand command = new(request);

        _productRepositoryMock
            .Setup(repository => repository.ExistsByNameOrSkuAsync(command.Request.Name, command.Request.Sku, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, false));

        Mock<IUnitOfWorkTransaction> transactionMock = CreateTransactionMock();

        _unitOfWorkMock
            .Setup(unit => unit.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactionMock.Object);

        _productRepositoryMock
            .Setup(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ApiResponse<CreateProductResponseDto> response = await _handler.Handle(command, CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.Equal("Product created successfully", response.Message);
        Assert.NotNull(response.Data);

        _productRepositoryMock.Verify(repository => repository.BulkInsertAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(unit => unit.CompleteAsync(), Times.Once);
        transactionMock.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        transactionMock.Verify(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CreateProductRequestDto CreateValidRequest() =>
        new()
        {
            Name = "Premium Hoodie",
            Description = "Cozy fleece hoodie",
            BasePrice = 79.99m,
            Sku = "HD-001",
            Options = new[]
            {
                new ProductOptionDto
                {
                    Name = "Color",
                    Values = new[] { "Black", "Gray" }
                },
                new ProductOptionDto
                {
                    Name = "Size",
                    Values = new[] { "S", "M", "L" }
                }
            },
            Variants = new[]
            {
                new ProductVariantDto
                {
                    Sku = "HD-001-BLK-S",
                    Price = 89.99m,
                    Stock = 25,
                    Selections = new[]
                    {
                        new VariantSelectionDto { OptionName = "Color", Value = "Black" },
                        new VariantSelectionDto { OptionName = "Size", Value = "S" }
                    }
                },
                new ProductVariantDto
                {
                    Sku = "HD-001-GRY-M",
                    Price = 89.99m,
                    Stock = 30,
                    Selections = new[]
                    {
                        new VariantSelectionDto { OptionName = "Color", Value = "Gray" },
                        new VariantSelectionDto { OptionName = "Size", Value = "M" }
                    }
                }
            }
        };

    private static Mock<IUnitOfWorkTransaction> CreateTransactionMock()
    {
        Mock<IUnitOfWorkTransaction> transactionMock = new();
        transactionMock.Setup(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(transaction => transaction.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transactionMock.Setup(transaction => transaction.DisposeAsync()).Returns(ValueTask.CompletedTask);
        return transactionMock;
    }
}

using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Application.Features.Products.Queries.GetAllProducts;
using Khaikhong.Application.Features.Products.Queries.GetProductById;
using Khaikhong.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace Khaikhong.Tests.Features.Products.Queries;

public sealed class ProductQueryHandlersTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<AutoMapper.IMapper> _mapperMock = new();
    private readonly Mock<ILogger<GetAllProductsQueryHandler>> _getAllLoggerMock = new();
    private readonly Mock<ILogger<GetProductByIdQueryHandler>> _getByIdLoggerMock = new();

    public ProductQueryHandlersTests()
    {
        _mapperMock
            .Setup(mapper => mapper.Map<List<ProductResponseDto>>(It.IsAny<object>()))
            .Returns((object source) =>
            {
                if (source is IEnumerable<Product> collection)
                {
                    return collection.Select(MapProduct).ToList();
                }

                return new List<ProductResponseDto>();
            });

        _mapperMock
            .Setup(mapper => mapper.Map<ProductResponseDto>(It.IsAny<Product>()))
            .Returns((Product product) => MapProduct(product));
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnMappedResponse()
    {
        IReadOnlyCollection<Product> products = new List<Product>
        {
            BuildProductWithVariant("Premium Hoodie")
        };

        _productRepositoryMock
            .Setup(repository => repository.GetAllDetailedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        GetAllProductsQueryHandler handler = new(
            _productRepositoryMock.Object,
            _mapperMock.Object,
            _getAllLoggerMock.Object);

        ApiResponse<List<ProductResponseDto>> response = await handler.Handle(new GetAllProductsQuery(), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.NotNull(response.Data);
        Assert.Single(response.Data);
        Assert.Equal("Premium Hoodie", response.Data![0].Name);
        Assert.Equal(120, response.Data[0].BaseStock);

        _productRepositoryMock.Verify(repository => repository.GetAllDetailedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnNotFound_WhenMissing()
    {
        Guid id = Guid.NewGuid();

        _productRepositoryMock
            .Setup(repository => repository.GetDetailedByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        GetProductByIdQueryHandler handler = new(
            _productRepositoryMock.Object,
            _mapperMock.Object,
            _getByIdLoggerMock.Object);

        ApiResponse<ProductResponseDto> response = await handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        Assert.False(response.IsSuccess);
        Assert.Equal(404, response.Status);
        _productRepositoryMock.Verify(repository => repository.GetDetailedByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProduct_WhenFound()
    {
        Product product = BuildProductWithVariant("Premium Hoodie");

        _productRepositoryMock
            .Setup(repository => repository.GetDetailedByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        GetProductByIdQueryHandler handler = new(
            _productRepositoryMock.Object,
            _mapperMock.Object,
            _getByIdLoggerMock.Object);

        ApiResponse<ProductResponseDto> response = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(200, response.Status);
        Assert.Equal(product.Name, response.Data!.Name);
        Assert.Single(response.Data.Options);
        Assert.Single(response.Data.Variants);

        _productRepositoryMock.Verify(repository => repository.GetDetailedByIdAsync(product.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Product BuildProductWithVariant(string name)
    {
        Product product = Product.Create(name, 79.99m, "Cozy fleece hoodie", "HD-001", 120);

        VariantOption option = VariantOption.Create(product.Id, "Color");
        VariantOptionValue value = VariantOptionValue.Create(option.Id, "Black");
        option.AddValues(new[] { value });
        product.AddOptions(new[] { option });

        Variant variant = Variant.Create(product.Id, 89.99m, 25, "HD-001-BLK-S");
        ProductVariantCombination combination = ProductVariantCombination.Create(variant.Id, value.Id);
        variant.AddCombinations(new[] { combination });
        product.AddVariants(new[] { variant });

        return product;
    }

    private static ProductResponseDto MapProduct(Product product)
    {
        ProductOptionResponseDto[] options = product.Options
            .Select(option => new ProductOptionResponseDto
            {
                Id = option.Id,
                Name = option.Name,
                Values = option.Values
                    .Select(value => new ProductOptionValueResponseDto
                    {
                        Id = value.Id,
                        Value = value.Value
                    })
                    .ToList()
            })
            .ToArray();

        ProductVariantResponseDto[] variants = product.Variants
            .Select(variant => new ProductVariantResponseDto
            {
                Id = variant.Id,
                Sku = variant.Sku,
                Price = variant.Price,
                Stock = variant.Stock,
                Combinations = variant.Combinations
                    .Select(combination => new ProductVariantCombinationResponseDto
                    {
                        Id = combination.Id,
                        OptionValueId = combination.OptionValueId
                    })
                    .ToList()
            })
            .ToArray();

        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            Sku = product.Sku,
            BaseStock = product.BaseStock,
            Options = options,
            Variants = variants
        };
    }
}

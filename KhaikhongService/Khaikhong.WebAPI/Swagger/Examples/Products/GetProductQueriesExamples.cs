using System.Collections.Generic;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Dtos;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Products;

public sealed class GetAllProductsSuccessResponseExample : IExamplesProvider<ApiResponse<List<ProductResponseDto>>>
{
    public ApiResponse<List<ProductResponseDto>> GetExamples() =>
        ApiResponse<List<ProductResponseDto>>.Success(
            status: StatusCodes.Status200OK,
            message: "Products retrieved successfully",
            data: new List<ProductResponseDto>
            {
                new()
                {
                    Id = Guid.Parse("8f6f6a77-0e99-4f04-9b69-0d05ea2a1eaa"),
                    Name = "Premium Hoodie",
                    Description = "Cozy fleece hoodie",
                    BasePrice = 79.99m,
                    Sku = "HD-001",
                    BaseStock = 120,
                    Options = new List<ProductOptionResponseDto>
                    {
                        new()
                        {
                            Id = Guid.Parse("1c3fa1b2-4c78-47e2-8b68-d1c4d9d8452f"),
                            Name = "Color",
                            Values = new List<ProductOptionValueResponseDto>
                            {
                                new() { Id = Guid.Parse("99887766-5544-3322-1100-aabbccddeeff"), Value = "Black" },
                                new() { Id = Guid.Parse("0f48a98f-4e45-4201-a8f2-5ab715b35fc4"), Value = "Gray" }
                            }
                        }
                    },
                    Variants = new List<ProductVariantResponseDto>
                    {
                        new()
                        {
                            Id = Guid.Parse("a1f65001-067f-4176-9868-d435b8456d10"),
                            Sku = "HD-001-BLK-S",
                            Price = 89.99m,
                            Stock = 25,
                            Combinations = new List<ProductVariantCombinationResponseDto>
                            {
                                new() { Id = Guid.Parse("b21b6b35-9c6f-4a09-881b-7e37d4db7b2c"), OptionValueId = Guid.Parse("99887766-5544-3322-1100-aabbccddeeff") }
                            }
                        }
                    }
                }
            });
}

public sealed class GetProductByIdSuccessResponseExample : IExamplesProvider<ApiResponse<ProductResponseDto>>
{
    public ApiResponse<ProductResponseDto> GetExamples() =>
        ApiResponse<ProductResponseDto>.Success(
            status: StatusCodes.Status200OK,
            message: "Product retrieved successfully",
            data: new ProductResponseDto
            {
                Id = Guid.Parse("8f6f6a77-0e99-4f04-9b69-0d05ea2a1eaa"),
                Name = "Premium Hoodie",
                Description = "Cozy fleece hoodie",
                BasePrice = 79.99m,
                Sku = "HD-001",
                BaseStock = 120,
                Options = new List<ProductOptionResponseDto>
                {
                    new()
                    {
                        Id = Guid.Parse("1c3fa1b2-4c78-47e2-8b68-d1c4d9d8452f"),
                        Name = "Color",
                        Values = new List<ProductOptionValueResponseDto>
                        {
                            new() { Id = Guid.Parse("99887766-5544-3322-1100-aabbccddeeff"), Value = "Black" },
                            new() { Id = Guid.Parse("0f48a98f-4e45-4201-a8f2-5ab715b35fc4"), Value = "Gray" }
                        }
                    }
                },
                Variants = new List<ProductVariantResponseDto>
                {
                    new()
                    {
                        Id = Guid.Parse("a1f65001-067f-4176-9868-d435b8456d10"),
                        Sku = "HD-001-BLK-S",
                        Price = 89.99m,
                        Stock = 25,
                        Combinations = new List<ProductVariantCombinationResponseDto>
                        {
                            new() { Id = Guid.Parse("b21b6b35-9c6f-4a09-881b-7e37d4db7b2c"), OptionValueId = Guid.Parse("99887766-5544-3322-1100-aabbccddeeff") }
                        }
                    }
                }
            });
}

public sealed class GetProductByIdNotFoundResponseExample : IExamplesProvider<ApiResponse<ProductResponseDto>>
{
    public ApiResponse<ProductResponseDto> GetExamples() =>
        ApiResponse<ProductResponseDto>.Fail(
            status: StatusCodes.Status404NotFound,
            message: "Product not found",
            errors: new[]
            {
                new { field = "productId", error = "Product does not exist." }
            });
}

public sealed class DeleteProductSuccessResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Success(
            status: StatusCodes.Status200OK,
            message: "Product deleted successfully",
            data: new { productId = Guid.Parse("8f6f6a77-0e99-4f04-9b69-0d05ea2a1eaa") });
}

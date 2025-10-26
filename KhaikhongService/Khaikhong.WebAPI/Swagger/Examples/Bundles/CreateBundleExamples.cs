using System;
using System.Collections.Generic;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Bundles.Dtos;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Bundles;

public sealed class CreateBundleSuccessResponseExample : IExamplesProvider<ApiResponse<CreateBundleResponseDto>>
{
    public ApiResponse<CreateBundleResponseDto> GetExamples() =>
        ApiResponse<CreateBundleResponseDto>.Success(
            status: StatusCodes.Status200OK,
            message: "Bundle created successfully",
            data: new CreateBundleResponseDto
            {
                Id = Guid.Parse("a6f2cbb1-3fef-4d1e-9d0a-2c209d7f2134"),
                Name = "Eco Starter Kit",
                Description = "A beginner-friendly sustainability bundle",
                Price = 1290m,
                ProductCount = 2,
                Items = new List<BundleItemResponseDto>
                {
                    new()
                    {
                        ProductId = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873"),
                        VariantId = null,
                        Quantity = 1
                    },
                    new()
                    {
                        ProductId = Guid.Parse("019a1cb4-f2db-7559-bf61-5d23ee22516e"),
                        VariantId = Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7142222"),
                        Quantity = 1
                    }
                }
            });
}

public sealed class CreateBundleValidationFailureResponseExample : IExamplesProvider<ApiResponse<CreateBundleResponseDto>>
{
    public ApiResponse<CreateBundleResponseDto> GetExamples() =>
        ApiResponse<CreateBundleResponseDto>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "request.products", error = "At least one product must be included in the bundle." }
            });
}

public sealed class CreateBundleRequestExample : IExamplesProvider<CreateBundleRequestDto>
{
    public CreateBundleRequestDto GetExamples() => new()
    {
        Name = "Eco Starter Kit",
        Description = "A beginner-friendly sustainability bundle",
        Price = 1290m,
        Products = new List<BundleProductDto>
        {
            new()
            {
                ProductId = Guid.Parse("019a1f40-3e0c-7bb1-a5a9-d4def7146873"),
                Quantity = 1
            },
            new()
            {
                ProductId = Guid.Parse("019a1cb4-f2db-7559-bf61-5d23ee22516e"),
                Quantity = 1,
                Variants = new List<Guid>
                {
                    Guid.Parse("019a1f40-aaaa-bbbb-cccc-d4def7142222")
                }
            }
        }
    };
}

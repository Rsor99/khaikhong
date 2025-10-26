using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Dtos;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Products;

public sealed class CreateProductSuccessResponseExample : IExamplesProvider<ApiResponse<CreateProductResponseDto>>
{
    public ApiResponse<CreateProductResponseDto> GetExamples() =>
        ApiResponse<CreateProductResponseDto>.Success(
            status: StatusCodes.Status200OK,
            message: "Product created successfully",
            data: new CreateProductResponseDto
            {
                Id = Guid.Parse("0f8907ce-8c41-4c1d-8108-f3733cd15fcc"),
                BasePrice = 1299.00m
            });
}

public sealed class CreateProductValidationFailureResponseExample : IExamplesProvider<ApiResponse<CreateProductResponseDto>>
{
    public ApiResponse<CreateProductResponseDto> GetExamples() =>
        ApiResponse<CreateProductResponseDto>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "Request.Name", error = "Product name already exists." },
                new { field = "Request.Options[0].Values", error = "Option values must be unique." }
            });
}

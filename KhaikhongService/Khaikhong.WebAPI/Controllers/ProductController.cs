using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Commands.CreateProduct;
using Khaikhong.Application.Features.Products.Commands.DeleteProduct;
using Khaikhong.Application.Features.Products.Commands.UpdateProduct;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Application.Features.Products.Queries.GetAllProducts;
using Khaikhong.Application.Features.Products.Queries.GetProductById;
using Khaikhong.WebAPI.Swagger.Examples.Common;
using Khaikhong.WebAPI.Swagger.Examples.Products;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ProductController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CreateProductSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(CreateProductValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request, CancellationToken cancellationToken)
    {
        ApiResponse<CreateProductResponseDto> response = await mediator.Send(new CreateProductCommand(request), cancellationToken);
        return StatusCode(response.Status, response);
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetAllProductsSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        ApiResponse<List<ProductResponseDto>> response = await mediator.Send(new GetAllProductsQuery(), cancellationToken);
        return StatusCode(response.Status, response);
    }

    [HttpGet("{productId:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetProductByIdSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(GetProductByIdNotFoundResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> GetProductById(Guid productId, CancellationToken cancellationToken)
    {
        ApiResponse<ProductResponseDto> response = await mediator.Send(new GetProductByIdQuery(productId), cancellationToken);
        return StatusCode(response.Status, response);
    }

    [HttpPut("{productId:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CreateProductResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CreateProductSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(CreateProductValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(GetProductByIdNotFoundResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductRequestDto request, CancellationToken cancellationToken)
    {
        UpdateProductRequestDto normalizedRequest = request with { ProductId = productId };

        ApiResponse<CreateProductResponseDto> response = await mediator.Send(
            new UpdateProductCommand(productId, normalizedRequest),
            cancellationToken);

        return StatusCode(response.Status, response);
    }

    [HttpDelete("{productId:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DeleteProductSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(GetProductByIdNotFoundResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> DeleteProduct(Guid productId, CancellationToken cancellationToken)
    {
        ApiResponse<object> response = await mediator.Send(new DeleteProductCommand(productId), cancellationToken);
        return StatusCode(response.Status, response);
    }
}

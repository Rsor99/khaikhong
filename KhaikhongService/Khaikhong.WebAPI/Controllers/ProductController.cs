using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Products.Commands.CreateProduct;
using Khaikhong.Application.Features.Products.Dtos;
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
}

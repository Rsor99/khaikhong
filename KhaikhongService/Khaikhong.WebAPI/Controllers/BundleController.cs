using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Bundles.Commands.CreateBundle;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.WebAPI.Swagger.Examples.Bundles;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Controllers;

[ApiController]
[Route("api/bundles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class BundleController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<CreateBundleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CreateBundleResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CreateBundleSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(CreateBundleValidationFailureResponseExample))]
    [SwaggerRequestExample(typeof(CreateBundleRequestDto), typeof(CreateBundleRequestExample))]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequestDto request, CancellationToken cancellationToken)
    {
        ApiResponse<CreateBundleResponseDto> response = await mediator.Send(new CreateBundleCommand(request), cancellationToken);
        return StatusCode(response.Status, response);
    }
}

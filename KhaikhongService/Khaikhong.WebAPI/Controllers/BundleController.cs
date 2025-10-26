using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Bundles.Commands.CreateBundle;
using Khaikhong.Application.Features.Bundles.Dtos;
using Khaikhong.Application.Features.Bundles.Commands.UpdateBundle;
using Khaikhong.Application.Features.Bundles.Commands.DeleteBundle;
using Khaikhong.Application.Features.Bundles.Queries.GetAllBundles;
using Khaikhong.Application.Features.Bundles.Queries.GetBundleById;
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
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<BundleResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBundles(CancellationToken cancellationToken)
    {
        ApiResponse<List<BundleResponseDto>> response = await mediator.Send(new GetAllBundlesQuery(), cancellationToken);
        return StatusCode(response.Status, response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBundleById(Guid id, CancellationToken cancellationToken)
    {
        ApiResponse<BundleResponseDto> response = await mediator.Send(new GetBundleByIdQuery(id), cancellationToken);
        return StatusCode(response.Status, response);
    }

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

    [HttpPut("{bundleId:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBundle(Guid bundleId, [FromBody] CreateBundleRequestDto request, CancellationToken cancellationToken)
    {
        ApiResponse<BundleResponseDto> response = await mediator.Send(new UpdateBundleCommand(bundleId, request), cancellationToken);
        return StatusCode(response.Status, response);
    }

    [HttpDelete("{bundleId:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<BundleResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBundle(Guid bundleId, CancellationToken cancellationToken)
    {
        ApiResponse<BundleResponseDto> response = await mediator.Send(new DeleteBundleCommand(bundleId), cancellationToken);
        return StatusCode(response.Status, response);
    }
}

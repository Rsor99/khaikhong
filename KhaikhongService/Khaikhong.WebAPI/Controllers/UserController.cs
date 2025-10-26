using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.User.Queries.GetCurrentUser;
using Khaikhong.WebAPI.Swagger.Examples.Common;
using Khaikhong.WebAPI.Swagger.Examples.User;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Controllers;

[ApiController]
[Route("api/user")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Authorize(Roles = "ADMIN,USER")]
public sealed class UserController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(CurrentUserSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(CurrentUserValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(CurrentUserUnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> GetCurrentUser()
    {
        ApiResponse<UserProfileDto> response = await mediator.Send(new GetCurrentUserQuery());
        return StatusCode(response.Status, response);
    }
}

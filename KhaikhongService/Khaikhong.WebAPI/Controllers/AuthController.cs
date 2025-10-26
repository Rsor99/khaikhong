using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Authentication.Commands.Login;
using Khaikhong.Application.Features.Authentication.Commands.Logout;
using Khaikhong.Application.Features.Authentication.Commands.RefreshToken;
using Khaikhong.Application.Features.Authentication.Commands.Register;
using Khaikhong.Application.Features.Authentication.Dtos;
using Khaikhong.Application.Models.Requests;
using Khaikhong.Infrastructure.Authentication;
using Khaikhong.WebAPI.Swagger.Examples.Authentication;
using Khaikhong.WebAPI.Swagger.Examples.Common;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator, JwtSettings jwtSettings) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(RegisterSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(RegisterValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        ApiResponse<RegisterResponseDto> response = await mediator.Send(new RegisterCommand(request));
        return StatusCode(response.Status, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(LoginSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(AuthenticationValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        ApiResponse<object> response = await mediator.Send(new LoginCommand(request.Email, request.Password));

        if (response.Status is >= 200 and < 400)
        {
            string? refreshToken = ExtractRefreshToken(response.Data);
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                WriteRefreshTokenCookie(refreshToken);
            }
        }

        return StatusCode(response.Status, response);
    }

    [HttpPost("refresh")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(RefreshSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(RefreshValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> Refresh()
    {
        if (!TryGetRefreshTokenCookie(out string? refreshToken))
        {
            ApiResponse<object> missingCookieResponse = BuildMissingRefreshTokenResponse();
            return StatusCode(missingCookieResponse.Status, missingCookieResponse);
        }

        ApiResponse<object> response = await mediator.Send(new RefreshTokenCommand(refreshToken ?? ""));

        if (response.Status is >= 200 and < 400)
        {
            string? newRefreshToken = ExtractRefreshToken(response.Data);
            if (!string.IsNullOrWhiteSpace(newRefreshToken))
            {
                WriteRefreshTokenCookie(newRefreshToken);
            }
        }

        return StatusCode(response.Status, response);
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(LogoutSuccessResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(LogoutValidationFailureResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status401Unauthorized, typeof(UnauthorizedResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status403Forbidden, typeof(ForbiddenResponseExample))]
    [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExample))]
    public async Task<IActionResult> Logout()
    {
        if (!TryGetRefreshTokenCookie(out string? refreshToken))
        {
            ApiResponse<object> missingCookieResponse = BuildMissingRefreshTokenResponse();
            return StatusCode(missingCookieResponse.Status, missingCookieResponse);
        }

        Guid? userId = ResolveUserId();
        ApiResponse<object> response = await mediator.Send(new LogoutCommand(userId, refreshToken ?? ""));

        if (response.Status is >= 200 and < 400)
        {
            Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/" });
        }

        return StatusCode(response.Status, response);
    }

    private Guid? ResolveUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(userIdValue, out Guid userId) ? userId : null;
    }

    private static string? ExtractRefreshToken(object? data)
    {
        if (data is null)
        {
            return null;
        }

        PropertyInfo? property = data.GetType().GetProperty("refreshToken", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetValue(data) as string;
    }

    private bool TryGetRefreshTokenCookie(out string? refreshToken)
    {
        refreshToken = null;

        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out string? value))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        refreshToken = value;
        return true;
    }

    private void WriteRefreshTokenCookie(string refreshToken)
    {
        CookieOptions options = BuildCookieOptions();
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, options);
    }

    private CookieOptions BuildCookieOptions()
    {
        DateTimeOffset expires = DateTimeOffset.UtcNow.AddDays(Math.Max(jwtSettings.RefreshTokenDays, 1));

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/"
        };
    }

    private static ApiResponse<object> BuildMissingRefreshTokenResponse() =>
        ApiResponse<object>.Fail(400, "Validation failed", errors: new { message = "Refresh token cookie is missing" });
}

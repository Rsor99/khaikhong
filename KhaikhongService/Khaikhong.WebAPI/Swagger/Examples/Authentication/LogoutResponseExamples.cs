using Khaikhong.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Authentication;

public sealed class LogoutSuccessResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Success(
            status: StatusCodes.Status200OK,
            message: "Logout successful",
            data: new { message = "Logged out" });
}

public sealed class LogoutValidationFailureResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "Cookies.RefreshToken", error = "Refresh token cookie is missing or invalid." }
            });
}

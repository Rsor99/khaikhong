using Khaikhong.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Authentication;

public sealed class LoginSuccessResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Success(
            status: StatusCodes.Status200OK,
            message: "Login successful",
            data: new
            {
                accessToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
                refreshToken = "4d7ab0d8c1714dd18f2a5635d06b8f2b"
            });
}

public sealed class AuthenticationValidationFailureResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "Request.Email", error = "'Request Email' is not a valid email address." },
                new { field = "Request.Password", error = "The length of 'Request Password' must be at least 8 characters. You entered 6 characters." }
            });
}

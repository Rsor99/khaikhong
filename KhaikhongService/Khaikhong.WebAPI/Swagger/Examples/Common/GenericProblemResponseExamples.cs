using Khaikhong.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Common;

public sealed class UnauthorizedResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Fail(
            status: StatusCodes.Status401Unauthorized,
            message: "Unauthorized - Missing or invalid token",
            errors: new { message = "Authentication is required to access this resource." });
}

public sealed class ForbiddenResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Fail(
            status: StatusCodes.Status403Forbidden,
            message: "Forbidden - You do not have permission",
            errors: new { message = "Your token is valid, but you do not have sufficient permissions." });
}

public sealed class InternalServerErrorResponseExample : IExamplesProvider<ApiResponse<object>>
{
    public ApiResponse<object> GetExamples() =>
        ApiResponse<object>.Fail(
            status: StatusCodes.Status500InternalServerError,
            message: "An unexpected error occurred",
            errors: new { traceId = Guid.Empty, message = "Something went wrong while processing the request." });
}

using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.User.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.User;

public sealed class CurrentUserSuccessResponseExample : IExamplesProvider<ApiResponse<UserProfileDto>>
{
    public ApiResponse<UserProfileDto> GetExamples() =>
        ApiResponse<UserProfileDto>.Success(
            status: StatusCodes.Status200OK,
            message: "Success",
            data: new UserProfileDto
            {
                FirstName = "John",
                LastName = "Doe",
                Role = "Admin"
            });
}

public sealed class CurrentUserValidationFailureResponseExample : IExamplesProvider<ApiResponse<UserProfileDto>>
{
    public ApiResponse<UserProfileDto> GetExamples() =>
        ApiResponse<UserProfileDto>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "Headers.Authorization", error = "Authorization header must contain a valid bearer token." }
            });
}

public sealed class CurrentUserUnauthorizedResponseExample : IExamplesProvider<ApiResponse<UserProfileDto>>
{
    public ApiResponse<UserProfileDto> GetExamples() =>
        ApiResponse<UserProfileDto>.Fail(
            status: StatusCodes.Status401Unauthorized,
            message: "Unauthorized - Missing or invalid token",
            errors: new[]
            {
                new { message = "The supplied bearer token is missing, expired, or malformed." }
            });
}

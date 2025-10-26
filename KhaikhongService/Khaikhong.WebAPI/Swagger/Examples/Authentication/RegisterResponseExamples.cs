using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Features.Authentication.Dtos;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.Filters;

namespace Khaikhong.WebAPI.Swagger.Examples.Authentication;

public sealed class RegisterSuccessResponseExample : IExamplesProvider<ApiResponse<RegisterResponseDto>>
{
    public ApiResponse<RegisterResponseDto> GetExamples() =>
        ApiResponse<RegisterResponseDto>.Success(
            status: StatusCodes.Status200OK,
            message: "Register successful",
            data: new RegisterResponseDto
            {
                UserId = Guid.Parse("2f9d3a85-8f33-4f4f-9c1d-a4c0c2bd7c4f"),
                Email = "user@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            });
}

public sealed class RegisterValidationFailureResponseExample : IExamplesProvider<ApiResponse<RegisterResponseDto>>
{
    public ApiResponse<RegisterResponseDto> GetExamples() =>
        ApiResponse<RegisterResponseDto>.Fail(
            status: StatusCodes.Status400BadRequest,
            message: "Validation failed",
            errors: new[]
            {
                new { field = "Request.Email", error = "'Request Email' is not a valid email address." },
                new { field = "Request.Password", error = "The length of 'Request Password' must be at least 8 characters. You entered 6 characters." },
                new { field = "Request.FirstName", error = "'Request First Name' must not be empty." },
                new { field = "Request.LastName", error = "'Request Last Name' must not be empty." },
                new { field = "Request.Role", error = "Role must be either 'User' or 'Admin'." }
            });
}

using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using MediatR;

namespace Khaikhong.Application.Features.Authentication;

public sealed record LogoutCommand(Guid? UserId, string RefreshToken) : IRequest<ApiResponse<object>>;

public sealed class LogoutCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LogoutCommand, ApiResponse<object>>
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    public async Task<ApiResponse<object>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId is null)
        {
            return AuthResult.Unauthorized("User is not authenticated").ToApiResponse();
        }

        await _authenticationService.LogoutAsync(request.UserId.Value, request.RefreshToken);

        var result = new AuthResult
        {
            Status = 200,
            Message = "Logout successful",
            Data = AuthResultData.ForMessage("Logged out")
        };

        return result.ToApiResponse();
    }
}

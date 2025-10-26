using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using MediatR;

namespace Khaikhong.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ApiResponse<object>>;

public sealed class RefreshTokenCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RefreshTokenCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        AuthResult result = await authenticationService.RefreshTokenAsync(request.RefreshToken);
        return result.ToApiResponse();
    }
}

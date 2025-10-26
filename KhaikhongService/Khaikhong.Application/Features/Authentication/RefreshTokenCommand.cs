using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using MediatR;

namespace Khaikhong.Application.Features.Authentication;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ApiResponse<object>>;

public sealed class RefreshTokenCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RefreshTokenCommand, ApiResponse<object>>
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    public async Task<ApiResponse<object>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        AuthResult result = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
        return result.ToApiResponse();
    }
}

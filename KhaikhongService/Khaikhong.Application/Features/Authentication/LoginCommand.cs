using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using MediatR;

namespace Khaikhong.Application.Features.Authentication;

public sealed record LoginCommand(string Email, string Password) : IRequest<ApiResponse<object>>;

public sealed class LoginCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LoginCommand, ApiResponse<object>>
{
    private readonly IAuthenticationService _authenticationService = authenticationService;

    public async Task<ApiResponse<object>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        AuthResult result = await _authenticationService.LoginAsync(request.Email, request.Password);
        return result.ToApiResponse();
    }
}

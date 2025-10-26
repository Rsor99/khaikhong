using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using MediatR;

namespace Khaikhong.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<ApiResponse<object>>;

public sealed class LoginCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LoginCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        AuthResult result = await authenticationService.LoginAsync(request.Email, request.Password);
        return result.ToApiResponse();
    }
}

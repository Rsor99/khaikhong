using Khaikhong.Application.Contracts.Services.Models;

namespace Khaikhong.Application.Contracts.Services;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string email, string password);

    Task<AuthResult> RefreshTokenAsync(string refreshToken);

    Task LogoutAsync(Guid userId, string refreshToken);
}

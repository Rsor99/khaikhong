using Khaikhong.Application.Contracts.Services;

namespace Khaikhong.Infrastructure.Services;

public sealed class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or whitespace.", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}

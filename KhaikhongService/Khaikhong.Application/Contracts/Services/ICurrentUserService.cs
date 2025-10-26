namespace Khaikhong.Application.Contracts.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? Role { get; }
}

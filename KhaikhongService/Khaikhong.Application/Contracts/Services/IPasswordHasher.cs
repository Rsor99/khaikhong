namespace Khaikhong.Application.Contracts.Services;

public interface IPasswordHasher
{
    string HashPassword(string password);
}

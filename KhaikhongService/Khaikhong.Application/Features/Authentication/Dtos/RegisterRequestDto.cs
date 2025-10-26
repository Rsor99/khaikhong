namespace Khaikhong.Application.Features.Authentication.Dtos;

public sealed class RegisterRequestDto
{
    public RegisterRequestDto()
    {
    }

    public RegisterRequestDto(string email, string password, string firstName, string lastName)
    {
        Email = email;
        Password = password;
        FirstName = firstName;
        LastName = lastName;
    }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;
}

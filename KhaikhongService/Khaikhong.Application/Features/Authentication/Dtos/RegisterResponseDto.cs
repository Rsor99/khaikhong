namespace Khaikhong.Application.Features.Authentication.Dtos;

public sealed class RegisterResponseDto
{
    public RegisterResponseDto()
    {
    }

    public RegisterResponseDto(Guid? userId, string? email, string? firstName, string? lastName, string? message = null)
    {
        UserId = userId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Message = message;
    }

    public Guid? UserId { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Message { get; init; }

    public bool ShouldSerializeUserId() => UserId.HasValue;

    public bool ShouldSerializeEmail() => !string.IsNullOrWhiteSpace(Email);

    public bool ShouldSerializeFirstName() => !string.IsNullOrWhiteSpace(FirstName);

    public bool ShouldSerializeLastName() => !string.IsNullOrWhiteSpace(LastName);

    public bool ShouldSerializeMessage() => !string.IsNullOrWhiteSpace(Message);
}

namespace Khaikhong.Application.Features.User.Queries.GetCurrentUser;

public sealed class UserProfileDto
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Role { get; init; }

    public string? Message { get; init; }

    public bool ShouldSerializeFirstName() => !string.IsNullOrWhiteSpace(FirstName);

    public bool ShouldSerializeLastName() => !string.IsNullOrWhiteSpace(LastName);

    public bool ShouldSerializeRole() => !string.IsNullOrWhiteSpace(Role);

    public bool ShouldSerializeMessage() => !string.IsNullOrWhiteSpace(Message);
}

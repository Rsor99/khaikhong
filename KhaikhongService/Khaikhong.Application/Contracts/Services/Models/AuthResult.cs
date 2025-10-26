namespace Khaikhong.Application.Contracts.Services.Models;

public sealed class AuthResult
{
    public int Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public AuthResultData Data { get; init; } = AuthResultData.Empty;

    public static AuthResult Success(string accessToken, string refreshToken, string? message = null) =>
        new()
        {
            Status = 200,
            Message = message ?? "Login successful",
            Data = AuthResultData.ForTokens(accessToken, refreshToken)
        };

    public static AuthResult Unauthorized(string message) =>
        new()
        {
            Status = 401,
            Message = "Unauthorized",
            Data = AuthResultData.ForError(message)
        };
}

public sealed class AuthResultData
{
    public static readonly AuthResultData Empty = new();

    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public string? Message { get; init; }

    public static AuthResultData ForTokens(string accessToken, string refreshToken) =>
        new()
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

    public static AuthResultData ForError(string message) =>
        new()
        {
            Message = message
        };

    public static AuthResultData ForMessage(string message) =>
        new()
        {
            Message = message
        };
}

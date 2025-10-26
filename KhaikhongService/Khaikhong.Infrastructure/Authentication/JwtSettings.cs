using System.Text;

namespace Khaikhong.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public int AccessTokenMinutes { get; init; } = 60;

    public int RefreshTokenDays { get; init; } = 7;

    public string PrivateKeyBase64 { get; init; } = string.Empty;

    public string PublicKeyBase64 { get; init; } = string.Empty;

    public string DecodePrivateKeyPem() => DecodeBase64ToString(PrivateKeyBase64, nameof(PrivateKeyBase64));

    public string DecodePublicKeyPem() => DecodeBase64ToString(PublicKeyBase64, nameof(PublicKeyBase64));

    private static string DecodeBase64ToString(string value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing JWT configuration for '{propertyName}'. Provide a base64-encoded PEM string in configuration.");
        }

        try
        {
            string sanitized = new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            byte[] bytes = Convert.FromBase64String(sanitized);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"The JWT configuration value '{propertyName}' is not valid base64.", ex);
        }
    }
}

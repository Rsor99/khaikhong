namespace Khaikhong.WebAPI.Configuration;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "ConfiguredCorsPolicy";

    public string[] AllowedOrigins { get; init; } = Array.Empty<string>();

    public bool AllowCredentials { get; init; }

    public string[] ExposedHeaders { get; init; } = Array.Empty<string>();
}

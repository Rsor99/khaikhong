using System.Security.Cryptography;
using Khaikhong.Application;
using Khaikhong.Infrastructure;
using Khaikhong.Infrastructure.Authentication;
using Khaikhong.WebAPI.Middlewares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load .env
Dictionary<string, string?> dotEnvValues = LoadDotEnv(builder.Environment.ContentRootPath);
if (dotEnvValues.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(dotEnvValues);
    foreach (var entry in dotEnvValues)
    {
        Environment.SetEnvironmentVariable(entry.Key, entry.Value);
    }
}

builder.Configuration.AddEnvironmentVariables();

// ✅ Add Application & Infrastructure layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ✅ JWT settings
var jwtSettingsSection = builder.Configuration.GetSection(JwtSettings.SectionName);
var jwtSettings = jwtSettingsSection.Get<JwtSettings>()
                  ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

var rsaSecurityKey = CreateRsaSecurityKey(jwtSettings.DecodePublicKeyPem());
var signingCredentials = new SigningCredentials(
    CreateRsaSecurityKey(jwtSettings.DecodePrivateKeyPem()),
    SecurityAlgorithms.RsaSha256);

builder.Services.AddSingleton(signingCredentials);

// ✅ Authentication config
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = rsaSecurityKey,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
    });

// ✅ Controllers + Swagger (Swashbuckle)
builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
return;

// 🔹 Helpers
static Dictionary<string, string?> LoadDotEnv(string rootPath)
{
    var envFilePath = Path.Combine(rootPath, ".env");
    var values = new Dictionary<string, string?>();

    if (!File.Exists(envFilePath))
        return values;

    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            continue;

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
            continue;

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if (key.Length > 0)
            values[key] = value;
    }

    return values;
}

static RsaSecurityKey CreateRsaSecurityKey(string pem)
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(pem);
    return new RsaSecurityKey(rsa);
}

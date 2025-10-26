using System.Net.Mime;
using System.Security.Cryptography;
using Khaikhong.Application;
using Khaikhong.Application.Common.Models;
using Khaikhong.Infrastructure;
using Khaikhong.Infrastructure.Authentication;
using Khaikhong.WebAPI.Middlewares;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;

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

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                ApiResponse<object> response = ApiResponse<object>.Fail(
                    status: StatusCodes.Status401Unauthorized,
                    message: "Unauthorized - Missing or invalid token",
                    errors: new[]
                    {
                        new { message = "The supplied bearer token is missing, expired, or malformed." }
                    });

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await WriteJsonAsync(context.HttpContext, response);
            },
            OnForbidden = context =>
            {
                if (context.Response.HasStarted)
                {
                    return Task.CompletedTask;
                }

                ApiResponse<object> response = ApiResponse<object>.Fail(
                    status: StatusCodes.Status403Forbidden,
                    message: "Forbidden - You do not have permission",
                    errors: new
                    {
                        message = "Your token is valid, but you do not have sufficient permissions."
                    });

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return WriteJsonAsync(context.HttpContext, response);
            }
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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Khaikhong API",
        Version = "v1",
        Description = "Secure API for authentication, user management, and product workflows."
    });

    options.ExampleFilters();

    OpenApiSecurityScheme bearerScheme = new()
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});
builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

var app = builder.Build();

// ✅ Middlewares
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Khaikhong API v1");
        options.RoutePrefix = "swagger";
    });
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

static Task WriteJsonAsync(HttpContext context, ApiResponse<object> response)
{
    context.Response.ContentType = MediaTypeNames.Application.Json;
    string payload = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    return context.Response.WriteAsync(payload);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Khaikhong.Application.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    private bool _initialized;
    private Guid? _userId;
    private string? _email;
    private string? _role;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            EnsureInitialized();
            return _userId;
        }
    }

    public string? Email
    {
        get
        {
            EnsureInitialized();
            return _email;
        }
    }

    public string? Role
    {
        get
        {
            EnsureInitialized();
            return _role;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        string? token = ResolveToken(context);
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        try
        {
            JwtSecurityToken jwt = _tokenHandler.ReadJwtToken(token);

            string? subject = jwt.Claims.FirstOrDefault(claim =>
                    claim.Type == JwtRegisteredClaimNames.Sub || claim.Type == ClaimTypes.NameIdentifier)
                ?.Value;
            string? email = jwt.Claims.FirstOrDefault(claim =>
                    claim.Type == JwtRegisteredClaimNames.Email || claim.Type == ClaimTypes.Email)
                ?.Value;
            string? role = jwt.Claims.FirstOrDefault(claim =>
                    claim.Type == ClaimTypes.Role || claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (Guid.TryParse(subject, out Guid parsedUserId))
            {
                _userId = parsedUserId;
            }

            _email = email;
            _role = role;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode JWT token for current user.");
            _userId = null;
            _email = null;
            _role = null;
        }
    }

    private static string? ResolveToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var authorizationValues))
        {
            string? bearerValue = authorizationValues.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(bearerValue) && bearerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return bearerValue["Bearer ".Length..].Trim();
            }
        }

        if (context.Request.Cookies.TryGetValue("access_token", out string? cookieToken) &&
            !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        return null;
    }
}

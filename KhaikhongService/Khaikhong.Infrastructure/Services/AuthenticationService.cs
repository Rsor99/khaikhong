using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Contracts.Services.Models;
using Khaikhong.Domain.Entities;
using Khaikhong.Infrastructure.Authentication;
using Khaikhong.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Khaikhong.Infrastructure.Services;

public sealed class AuthenticationService(
    IdentityDbContext identityDbContext,
    JwtSettings jwtSettings,
    SigningCredentials signingCredentials,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly TimeSpan _refreshTokenLifetime = TimeSpan.FromDays(Math.Max(jwtSettings.RefreshTokenDays, 1));

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Unauthorized("Email or password is incorrect");
        }

        User? user = await identityDbContext.Users
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            logger.LogWarning("Login failed for {Email}", email);
            return AuthResult.Unauthorized("Email or password is incorrect");
        }

        bool passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogWarning("Login failed for user {UserId}: invalid password", user.Id);
            return AuthResult.Unauthorized("Password is incorrect");
        }

        (string accessToken, string refreshToken) = await IssueTokensAsync(user);

        return AuthResult.Success(accessToken, refreshToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        if (!TryExtractTokenId(refreshToken, out string? tokenId))
        {
            return AuthResult.Unauthorized("Refresh token is invalid");
        }

        RefreshToken? existingToken = await identityDbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenId == tokenId);

        if (existingToken is null)
        {
            return AuthResult.Unauthorized("Refresh token is invalid");
        }

        if (!existingToken.IsActiveToken(DateTime.UtcNow))
        {
            return AuthResult.Unauthorized("Refresh token is expired or revoked");
        }

        if (!BCrypt.Net.BCrypt.Verify(refreshToken, existingToken.TokenHash))
        {
            return AuthResult.Unauthorized("Refresh token is invalid");
        }

        User user = existingToken.User;
        existingToken.Revoke();

        (string accessToken, string newRefreshToken) = await IssueTokensAsync(user);

        return AuthResult.Success(accessToken, newRefreshToken, "Token refreshed");
    }

    public async Task LogoutAsync(Guid userId, string refreshToken)
    {
        if (!TryExtractTokenId(refreshToken, out string? tokenId))
        {
            return;
        }

        RefreshToken? existingToken = await identityDbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.TokenId == tokenId && token.RevokedAt == null)
            .SingleOrDefaultAsync();

        if (existingToken is null)
        {
            return;
        }

        if (!BCrypt.Net.BCrypt.Verify(refreshToken, existingToken.TokenHash))
        {
            return;
        }

        existingToken.Revoke();
        await identityDbContext.SaveChangesAsync();
    }

    private async Task<(string AccessToken, string RefreshToken)> IssueTokensAsync(User user)
    {
        string accessToken = GenerateAccessToken(user);
        (RefreshToken Entity, string Value) refreshToken = GenerateRefreshToken(user);

        identityDbContext.RefreshTokens.Add(refreshToken.Entity);
        await identityDbContext.SaveChangesAsync();

        return (accessToken, refreshToken.Value);
    }

    private string GenerateAccessToken(User user)
    {
        DateTime utcNow = DateTime.UtcNow;
        DateTime expires = utcNow.AddMinutes(Math.Max(jwtSettings.AccessTokenMinutes, 1));

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        ];

        JwtSecurityToken jwt = new(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            notBefore: utcNow.Subtract(ClockSkew),
            expires: expires,
            signingCredentials: signingCredentials);

        return _tokenHandler.WriteToken(jwt);
    }

    private (RefreshToken Entity, string Value) GenerateRefreshToken(User user)
    {
        string tokenId = Guid.CreateVersion7().ToString("N");
        string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        string rawToken = $"{tokenId}.{secret}";

        string hash = BCrypt.Net.BCrypt.HashPassword(rawToken);

        DateTime expiresAt = DateTime.UtcNow.Add(_refreshTokenLifetime);
        var entity = RefreshToken.Create(user.Id, tokenId, hash, expiresAt);
        entity.AttachUser(user);

        return (entity, rawToken);
    }

    private static bool TryExtractTokenId(string value, out string? tokenId)
    {
        tokenId = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string[] parts = value.Split('.', 2, StringSplitOptions.TrimEntries);

        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        tokenId = parts[0];
        return true;
    }
}

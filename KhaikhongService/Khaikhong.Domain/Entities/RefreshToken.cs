using Khaikhong.Domain.Common;

namespace Khaikhong.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }

    public string TokenId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken(Guid userId, string tokenId, string tokenHash, DateTime expiresAt)
    {
        UserId = userId;
        TokenId = tokenId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public static RefreshToken Create(Guid userId, string tokenId, string tokenHash, DateTime expiresAt) =>
        new(userId, tokenId, tokenHash, expiresAt);

    public void AttachUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id != UserId)
        {
            throw new InvalidOperationException("Cannot attach a user that does not own this refresh token.");
        }

        User = user;
    }

    public bool IsActiveToken(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow && base.IsActive;

    public void Revoke()
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = DateTime.UtcNow;
        Deactivate();
    }
}

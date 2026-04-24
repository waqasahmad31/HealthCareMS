using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class UserAuthSession : BaseEntity
{
    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceLabel { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

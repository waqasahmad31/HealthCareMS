using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class ApplicationUser : BaseEntity
{
    public Guid? TenantId { get; set; }

    public Guid RoleId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsEmailVerified { get; set; }

    public short FailedLoginCount { get; set; }

    public DateTimeOffset? LockoutUntil { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? RefreshTokenExpiry { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public string? TwoFactorSecret { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public Tenant? Tenant { get; set; }

    public Role Role { get; set; } = null!;

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

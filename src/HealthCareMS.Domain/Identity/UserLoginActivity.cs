using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class UserLoginActivity : BaseEntity
{
    public Guid? UserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; }

    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? FailureReason { get; set; }

    public ApplicationUser? User { get; set; }
}

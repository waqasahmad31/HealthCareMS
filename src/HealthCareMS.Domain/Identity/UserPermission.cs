namespace HealthCareMS.Domain.Identity;

public sealed class UserPermission
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsGranted { get; set; } = true;

    public Guid GrantedByUserId { get; set; }

    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; set; } = null!;

    public Permission Permission { get; set; } = null!;

    public ApplicationUser GrantedByUser { get; set; } = null!;
}

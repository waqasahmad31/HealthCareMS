using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class Role : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSystemRole { get; set; }

    public Tenant? Tenant { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

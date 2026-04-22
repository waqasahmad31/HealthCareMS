using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class Permission : BaseEntity
{
    public string PermissionKey { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}

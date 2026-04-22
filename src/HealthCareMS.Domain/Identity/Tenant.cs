using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public enum TenantType
{
    Pharmacy = 1,
    Lab = 2,
    Clinic = 3,
    Hospital = 4
}

public enum SubscriptionPlan
{
    Basic = 1,
    Standard = 2,
    Premium = 3
}

public sealed class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TenantType TenantType { get; set; }

    public string? LicenseNumber { get; set; }

    public string OwnerName { get; set; } = string.Empty;

    public string? OwnerCnic { get; set; }

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Basic;

    public int MaxUsers { get; set; } = 2;

    public string EnabledModules { get; set; } = "{}";

    public string PaymentGateways { get; set; } = "{}";

    public Guid? CreatedBySuperAdminId { get; set; }

    public ApplicationUser? CreatedBySuperAdmin { get; set; }

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

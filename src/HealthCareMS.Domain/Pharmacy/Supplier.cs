using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Pharmacy;

public sealed class Supplier : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}

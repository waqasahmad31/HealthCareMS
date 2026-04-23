using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Labs;

public sealed class LabTest : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string TestCode { get; set; } = string.Empty;

    public string TestName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string SampleType { get; set; } = "Blood";

    public short TurnaroundHours { get; set; } = 24;

    public short? FastingHours { get; set; }

    public string? PreparationInstructions { get; set; }

    public decimal Price { get; set; }

    public bool IsHomeCollectionAvailable { get; set; }

    public decimal HomeCollectionExtra { get; set; }

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}

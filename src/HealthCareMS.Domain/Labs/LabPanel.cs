using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Labs;

public sealed class LabPanel : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string PanelCode { get; set; } = string.Empty;

    public string PanelName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }

    public ICollection<LabPanelItem> Items { get; set; } = new List<LabPanelItem>();
}

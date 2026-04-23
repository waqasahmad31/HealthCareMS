using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class NavigationGroup : BaseEntity
{
    public Guid? TenantId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string LabelEn { get; set; } = string.Empty;

    public string LabelUr { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }

    public ICollection<NavigationItem> Items { get; set; } = new List<NavigationItem>();
}

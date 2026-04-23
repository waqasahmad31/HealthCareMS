using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class NavigationItem : BaseEntity
{
    public Guid NavigationGroupId { get; set; }

    public Guid? ParentItemId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string LabelEn { get; set; } = string.Empty;

    public string LabelUr { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string RequiredPermissionsJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public NavigationGroup NavigationGroup { get; set; } = null!;

    public NavigationItem? ParentItem { get; set; }

    public ICollection<NavigationItem> Children { get; set; } = new List<NavigationItem>();
}

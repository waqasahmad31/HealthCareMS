using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class NavigationIcon : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string LabelEn { get; set; } = string.Empty;

    public string LabelUr { get; set; } = string.Empty;

    public string? CssClass { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

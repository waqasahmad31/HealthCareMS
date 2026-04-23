using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class NavigationIcon : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

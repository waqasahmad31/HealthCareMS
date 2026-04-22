using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Identity;

public sealed class SystemSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;

    public string GroupName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string ValueType { get; set; } = "String";

    public string? Description { get; set; }

    public bool IsSensitive { get; set; }

    public bool IsEditable { get; set; } = true;
}

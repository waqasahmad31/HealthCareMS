namespace HealthCareMS.Application.Admin;

public sealed record UpdateDoctorAdminStatusRequest(bool IsVerified, bool IsActive);

public sealed record UpdateSystemSettingRequest(string Value);

public sealed record UpdateNavigationConfigurationRequest(string ConfigurationJson);

public sealed record CreateNavigationGroupRequest(
    string Key,
    string LabelEn,
    string LabelUr,
    int SortOrder,
    bool IsActive = true);

public sealed record UpdateNavigationGroupRequest(
    string LabelEn,
    string LabelUr,
    int SortOrder,
    bool IsActive);

public sealed record CreateNavigationItemRequest(
    Guid NavigationGroupId,
    Guid? ParentItemId,
    string Key,
    string LabelEn,
    string LabelUr,
    string Icon,
    string Route,
    int SortOrder,
    IReadOnlyList<string>? RequiredPermissions,
    bool IsActive = true);

public sealed record UpdateNavigationItemRequest(
    Guid NavigationGroupId,
    Guid? ParentItemId,
    string LabelEn,
    string LabelUr,
    string Icon,
    string Route,
    int SortOrder,
    IReadOnlyList<string>? RequiredPermissions,
    bool IsActive);

public sealed record CreateNavigationIconRequest(
    string Key,
    string Symbol,
    string? Description,
    bool IsActive = true);

public sealed record UpdateNavigationIconRequest(
    string Symbol,
    string? Description,
    bool IsActive);

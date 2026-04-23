namespace HealthCareMS.Blazor.Models;

public sealed record AdminAppointmentRowModel(
    Guid Id,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    string Type,
    string Status,
    string Priority,
    string? ReasonForVisit,
    decimal ConsultationFee,
    string PaymentStatus,
    int? QueueNumber,
    DateTimeOffset CreatedAt);

public sealed record AdminAppointmentOverviewModel(
    int TotalCount,
    int PageNumber,
    int PageSize,
    int PendingCount,
    int ConfirmedCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    decimal CompletedFeeTotal,
    IReadOnlyList<AdminAppointmentRowModel> Appointments);

public sealed record AdminDoctorRowModel(
    Guid Id,
    Guid UserId,
    Guid? TenantId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string PmdcRegistrationNumber,
    string Specialization,
    string City,
    decimal ConsultationFee,
    bool IsVerified,
    bool IsActive,
    int ScheduleCount,
    DateTimeOffset CreatedAt);

public sealed record AdminDoctorManagementModel(
    int TotalCount,
    int VerifiedCount,
    int PendingVerificationCount,
    int ActiveCount,
    int InactiveCount,
    IReadOnlyList<AdminDoctorRowModel> Doctors);

public sealed record PermissionModel(
    Guid Id,
    string PermissionKey,
    string Module,
    string Action,
    string Description);

public sealed record SystemSettingModel(
    Guid Id,
    string SettingKey,
    string GroupName,
    string DisplayName,
    string Value,
    string ValueType,
    string? Description,
    bool IsSensitive,
    bool IsEditable,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record SystemConfigurationModel(IReadOnlyList<SystemSettingModel> Settings);

public sealed class UpdateDoctorAdminStatusModel
{
    public bool IsVerified { get; set; }

    public bool IsActive { get; set; }
}

public sealed class UpdateSystemSettingModel
{
    public string Value { get; set; } = string.Empty;
}

public sealed record NavigationConfigurationModel(string ConfigurationJson);

public sealed class UpdateNavigationConfigurationModel
{
    public string ConfigurationJson { get; set; } = string.Empty;
}

public sealed record UserMenuAssignmentModel(
    Guid UserId,
    IReadOnlyList<string> MenuItemKeys,
    DateTimeOffset UpdatedAt);

public sealed record ManageableUserModel(
    Guid Id,
    Guid? TenantId,
    Guid RoleId,
    string Role,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid? CreatedByUserId,
    bool IsActive,
    bool IsEmailVerified);

public sealed class AssignUserMenuModel
{
    public IReadOnlyList<string> MenuItemKeys { get; set; } = [];
}

public sealed record NavigationGroupModel(
    Guid Id,
    Guid? TenantId,
    string Key,
    string LabelEn,
    string LabelUr,
    int SortOrder,
    bool IsActive);

public sealed class CreateNavigationGroupModel
{
    public string Key { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateNavigationGroupModel
{
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record NavigationItemModel(
    Guid Id,
    Guid NavigationGroupId,
    Guid? ParentItemId,
    string Key,
    string LabelEn,
    string LabelUr,
    string Icon,
    string Route,
    int SortOrder,
    IReadOnlyList<string> RequiredPermissions,
    bool IsActive,
    IReadOnlyList<NavigationItemModel> Children);

public sealed class CreateNavigationItemModel
{
    public Guid NavigationGroupId { get; set; }
    public Guid? ParentItemId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<string> RequiredPermissions { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateNavigationItemModel
{
    public Guid NavigationGroupId { get; set; }
    public Guid? ParentItemId { get; set; }
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public IReadOnlyList<string> RequiredPermissions { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public sealed record NavigationIconModel(
    Guid Id,
    string Key,
    string LabelEn,
    string LabelUr,
    string? CssClass,
    string Symbol,
    string? Description,
    bool IsActive);

public sealed class CreateNavigationIconModel
{
    public string Key { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public string? CssClass { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateNavigationIconModel
{
    public string LabelEn { get; set; } = string.Empty;
    public string LabelUr { get; set; } = string.Empty;
    public string? CssClass { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

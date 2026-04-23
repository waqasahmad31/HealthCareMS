namespace HealthCareMS.Application.Admin;

public sealed record AdminAppointmentRowResponse(
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

public sealed record AdminAppointmentOverviewResponse(
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
    IReadOnlyList<AdminAppointmentRowResponse> Appointments);

public sealed record AdminDoctorRowResponse(
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

public sealed record AdminDoctorManagementResponse(
    int TotalCount,
    int VerifiedCount,
    int PendingVerificationCount,
    int ActiveCount,
    int InactiveCount,
    IReadOnlyList<AdminDoctorRowResponse> Doctors);

public sealed record SystemSettingResponse(
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

public sealed record SystemConfigurationResponse(IReadOnlyList<SystemSettingResponse> Settings);

public sealed record NavigationMenuResponse(
    string Culture,
    IReadOnlyList<NavigationMenuGroupResponse> Groups);

public sealed record NavigationMenuGroupResponse(
    string Key,
    string Label,
    int SortOrder,
    IReadOnlyList<NavigationMenuItemResponse> Items);

public sealed record NavigationMenuItemResponse(
    string Key,
    string Label,
    string Icon,
    string Route,
    int SortOrder,
    IReadOnlyList<NavigationMenuItemResponse> Children);

public sealed record NavigationConfigurationResponse(string ConfigurationJson);

public sealed record NavigationGroupResponse(
    Guid Id,
    Guid? TenantId,
    string Key,
    string LabelEn,
    string LabelUr,
    int SortOrder,
    bool IsActive);

public sealed record NavigationIconResponse(
    Guid Id,
    string Key,
    string Symbol,
    string? Description,
    bool IsActive);

public sealed record NavigationItemResponse(
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
    IReadOnlyList<NavigationItemResponse> Children);

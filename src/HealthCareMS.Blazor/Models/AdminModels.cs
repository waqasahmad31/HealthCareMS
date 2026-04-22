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

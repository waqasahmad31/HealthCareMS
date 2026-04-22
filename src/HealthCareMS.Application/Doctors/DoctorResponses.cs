namespace HealthCareMS.Application.Doctors;

public sealed record DoctorResponse(
    Guid Id,
    Guid UserId,
    Guid? TenantId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string PmdcRegistrationNumber,
    string Specialization,
    string? Qualification,
    string? Biography,
    string City,
    decimal ConsultationFee,
    bool IsVerified,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DoctorScheduleResponse> Schedules);

public sealed record DoctorScheduleResponse(
    Guid Id,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    short SlotDurationMinutes,
    bool IsOnlineAvailable,
    bool IsOnSiteAvailable);

public sealed record AvailableSlotResponse(TimeOnly StartTime, TimeOnly EndTime, string AppointmentType);

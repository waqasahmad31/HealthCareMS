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
    decimal AverageRating,
    int RatingCount,
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

public sealed record DoctorReviewResponse(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    byte Rating,
    string? ReviewText,
    bool IsRecommended,
    DateTimeOffset ReviewedAt,
    DateTimeOffset CreatedAt);

public sealed record DoctorRatingSummaryResponse(
    Guid DoctorId,
    string DoctorName,
    decimal AverageRating,
    int RatingCount,
    IReadOnlyList<DoctorReviewResponse> RecentReviews);

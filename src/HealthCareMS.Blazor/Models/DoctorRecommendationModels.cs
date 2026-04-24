namespace HealthCareMS.Blazor.Models;

public sealed record DoctorRecommendationModel(
    DoctorModel Doctor,
    decimal MatchScore,
    bool IsBestMatch,
    int AvailableSlotCount,
    IReadOnlyList<string> MatchReasons);

public sealed record DoctorModel(
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
    IReadOnlyList<DoctorScheduleModel> Schedules);

public sealed record DoctorScheduleModel(
    Guid Id,
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    short SlotDurationMinutes,
    bool IsOnlineAvailable,
    bool IsOnSiteAvailable);

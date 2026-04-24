namespace HealthCareMS.Application.Doctors;

public sealed record CreateDoctorProfileRequest(
    Guid? TenantId,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string PmdcRegistrationNumber,
    string Specialization,
    string? Qualification,
    string? Biography,
    string City,
    decimal ConsultationFee);

public sealed record UpdateDoctorProfileRequest(
    string Specialization,
    string? Qualification,
    string? Biography,
    string City,
    decimal ConsultationFee,
    bool IsActive);

public sealed record VerifyDoctorRequest(bool IsVerified);

public sealed record SetDoctorScheduleRequest(IReadOnlyList<DoctorScheduleSlotRequest> Slots);

public sealed record DoctorScheduleSlotRequest(
    string DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    short SlotDurationMinutes,
    bool IsOnlineAvailable,
    bool IsOnSiteAvailable);

public sealed record DoctorRecommendationRequest(
    Guid? PatientId,
    string? Specialization,
    string? City,
    decimal? MaxFee,
    DateOnly? Date,
    string AppointmentType = "Online");

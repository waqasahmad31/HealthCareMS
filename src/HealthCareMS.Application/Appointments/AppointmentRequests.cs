namespace HealthCareMS.Application.Appointments;

public sealed record BookAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    DateTimeOffset ScheduledAt,
    string Type,
    short DurationMinutes,
    string ReasonForVisit,
    string? Priority,
    string? PatientNotes);

public sealed record RescheduleAppointmentRequest(
    DateTimeOffset ScheduledAt,
    short DurationMinutes,
    string? Reason);

public sealed record CancelAppointmentRequest(
    string CancellationReason,
    string CancelledBy);

public sealed record CompleteAppointmentRequest(
    string Diagnosis,
    string? ClinicalNotes,
    DateOnly? FollowUpDate);

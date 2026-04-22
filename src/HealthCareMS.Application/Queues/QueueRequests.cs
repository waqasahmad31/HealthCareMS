namespace HealthCareMS.Application.Queues;

public sealed record WalkInRegistrationRequest(
    Guid PatientId,
    Guid DoctorId,
    string ReasonForVisit,
    string? Priority,
    string? PatientNotes);

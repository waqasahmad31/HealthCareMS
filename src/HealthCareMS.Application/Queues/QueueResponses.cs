namespace HealthCareMS.Application.Queues;

public sealed record QueueEntryResponse(
    Guid AppointmentId,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    int QueueNumber,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset ScheduledAt,
    string Status,
    string Priority,
    string ReasonForVisit,
    int Position,
    int EstimatedWaitMinutes);

public sealed record QueueBoardResponse(
    Guid DoctorId,
    string DoctorName,
    DateOnly Date,
    int WaitingCount,
    int InProgressCount,
    int CompletedCount,
    int AverageSlotMinutes,
    QueueEntryResponse? NextPatient,
    IReadOnlyList<QueueEntryResponse> Entries,
    DateTimeOffset RefreshedAt);

public sealed record PatientQueueStatusResponse(
    Guid AppointmentId,
    string AppointmentNumber,
    Guid DoctorId,
    string DoctorName,
    DateOnly Date,
    int? QueueNumber,
    string Status,
    int Position,
    int EstimatedWaitMinutes,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset RefreshedAt);

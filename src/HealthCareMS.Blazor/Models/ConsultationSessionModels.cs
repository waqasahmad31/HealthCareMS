namespace HealthCareMS.Blazor.Models;

public sealed record ConsultationSessionModel(
    Guid Id,
    Guid AppointmentId,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string ChannelName,
    string MeetingLink,
    string Status,
    DateTimeOffset? PatientJoinedAt,
    DateTimeOffset? DoctorJoinedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset CreatedAt);

public sealed record JoinConsultationSessionModel(
    ConsultationSessionModel Session,
    string ParticipantType,
    string AgoraAppId,
    string ChannelName,
    string Token,
    int Uid,
    DateTimeOffset ExpiresAt,
    bool PatientIsOnline,
    bool DoctorIsOnline);

public sealed class StartConsultationSessionModel
{
    public Guid AppointmentId { get; set; }
}

public sealed class JoinConsultationSessionRequestModel
{
    public string ParticipantType { get; set; } = "Patient";
}

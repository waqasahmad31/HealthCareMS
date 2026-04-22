namespace HealthCareMS.Application.Consultations;

public sealed record ConsultationSessionResponse(
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

public sealed record JoinConsultationSessionResponse(
    ConsultationSessionResponse Session,
    string ParticipantType,
    string AgoraAppId,
    string ChannelName,
    string Token,
    int Uid,
    DateTimeOffset ExpiresAt,
    bool PatientIsOnline,
    bool DoctorIsOnline);

namespace HealthCareMS.Application.Consultations;

public sealed record StartConsultationSessionRequest(Guid AppointmentId);

public sealed record JoinConsultationSessionRequest(string ParticipantType);

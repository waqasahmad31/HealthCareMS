using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Consultations;

public interface IConsultationSessionService
{
    Task<Result<ConsultationSessionResponse>> StartAsync(StartConsultationSessionRequest request, CancellationToken cancellationToken);

    Task<Result<ConsultationSessionResponse>> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<Result<ConsultationSessionResponse>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<Result<JoinConsultationSessionResponse>> JoinAsync(Guid sessionId, JoinConsultationSessionRequest request, CancellationToken cancellationToken);
}

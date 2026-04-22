namespace HealthCareMS.Application.Consultations;

public interface IConsultationSessionNotifier
{
    Task NotifySessionChangedAsync(ConsultationSessionResponse session, string eventName, CancellationToken cancellationToken);
}

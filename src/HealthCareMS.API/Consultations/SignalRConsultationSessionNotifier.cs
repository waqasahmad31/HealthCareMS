using HealthCareMS.API.Hubs;
using HealthCareMS.Application.Consultations;
using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Consultations;

public sealed class SignalRConsultationSessionNotifier(IHubContext<ConsultationHub> hubContext) : IConsultationSessionNotifier
{
    public async Task NotifySessionChangedAsync(
        ConsultationSessionResponse session,
        string eventName,
        CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(ConsultationHub.GroupName(session.Id))
            .SendAsync("SessionChanged", new { EventName = eventName, Session = session }, cancellationToken);
    }
}

using HealthCareMS.API.Hubs;
using HealthCareMS.Application.Consultations;
using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Consultations;

public sealed class SignalRConsultationChatNotifier(IHubContext<ConsultationChatHub> hubContext) : IConsultationChatNotifier
{
    public async Task NotifyMessageSentAsync(ChatMessageResponse message, CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(ConsultationChatHub.GroupName(message.SessionId))
            .SendAsync("MessageReceived", message, cancellationToken);
    }

    public async Task NotifyReadReceiptsUpdatedAsync(
        Guid sessionId,
        IReadOnlyList<ChatMessageResponse> messages,
        CancellationToken cancellationToken)
    {
        await hubContext.Clients
            .Group(ConsultationChatHub.GroupName(sessionId))
            .SendAsync("ReadReceiptsUpdated", messages, cancellationToken);
    }
}

namespace HealthCareMS.Application.Consultations;

public interface IConsultationChatNotifier
{
    Task NotifyMessageSentAsync(ChatMessageResponse message, CancellationToken cancellationToken);

    Task NotifyReadReceiptsUpdatedAsync(Guid sessionId, IReadOnlyList<ChatMessageResponse> messages, CancellationToken cancellationToken);
}

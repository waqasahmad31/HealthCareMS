using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Consultations;

public interface IConsultationChatService
{
    Task<Result<IReadOnlyList<ChatMessageResponse>>> GetMessagesAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<Result<ChatMessageResponse>> SendMessageAsync(
        Guid sessionId,
        SendChatMessageRequest request,
        Guid? senderUserId,
        CancellationToken cancellationToken);

    Task<Result<ChatMessageResponse>> UploadAttachmentAsync(
        Guid sessionId,
        UploadChatAttachmentRequest request,
        Guid? senderUserId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChatMessageResponse>>> MarkReadAsync(
        Guid sessionId,
        MarkChatMessagesReadRequest request,
        CancellationToken cancellationToken);

    Task<Result<ChatAttachmentDownloadResponse>> DownloadAttachmentAsync(Guid messageId, CancellationToken cancellationToken);
}

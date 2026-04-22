using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Consultations;

public interface IChatFileStorage
{
    Task<StoredChatFile> SaveAsync(
        Guid sessionId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<Result<ChatAttachmentDownloadResponse>> OpenReadAsync(
        string storagePath,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}

public sealed record StoredChatFile(string StoragePath, string FileName, string ContentType, long SizeBytes);

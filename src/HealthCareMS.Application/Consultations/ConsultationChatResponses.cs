namespace HealthCareMS.Application.Consultations;

public sealed record ChatMessageResponse(
    Guid Id,
    Guid SessionId,
    Guid? SenderUserId,
    string SenderType,
    string SenderDisplayName,
    string MessageType,
    string? MessageText,
    string? AttachmentFileName,
    string? AttachmentContentType,
    long? AttachmentSizeBytes,
    string? DownloadUrl,
    DateTimeOffset SentAt,
    DateTimeOffset? PatientReadAt,
    DateTimeOffset? DoctorReadAt);

public sealed record ChatAttachmentDownloadResponse(
    Stream Content,
    string FileName,
    string ContentType);

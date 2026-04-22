namespace HealthCareMS.Application.Consultations;

public sealed record SendChatMessageRequest(
    string ParticipantType,
    string SenderDisplayName,
    string MessageText);

public sealed record UploadChatAttachmentRequest(
    string ParticipantType,
    string SenderDisplayName,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record MarkChatMessagesReadRequest(string ParticipantType);

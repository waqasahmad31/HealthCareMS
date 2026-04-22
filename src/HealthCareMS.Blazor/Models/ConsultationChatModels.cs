namespace HealthCareMS.Blazor.Models;

public sealed record ChatMessageModel(
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

public sealed class SendChatMessageModel
{
    public string ParticipantType { get; set; } = "Patient";

    public string SenderDisplayName { get; set; } = string.Empty;

    public string MessageText { get; set; } = string.Empty;
}

public sealed class MarkChatMessagesReadModel
{
    public string ParticipantType { get; set; } = "Patient";
}

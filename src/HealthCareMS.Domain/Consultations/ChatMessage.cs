using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Consultations;

public enum ChatMessageType
{
    Text = 1,
    File = 2
}

public sealed class ChatMessage : BaseEntity
{
    public Guid SessionId { get; set; }

    public Guid? SenderUserId { get; set; }

    public string SenderType { get; set; } = string.Empty;

    public string SenderDisplayName { get; set; } = string.Empty;

    public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

    public string? MessageText { get; set; }

    public string? AttachmentFileName { get; set; }

    public string? AttachmentContentType { get; set; }

    public string? AttachmentStoragePath { get; set; }

    public long? AttachmentSizeBytes { get; set; }

    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PatientReadAt { get; set; }

    public DateTimeOffset? DoctorReadAt { get; set; }

    public ConsultationSession Session { get; set; } = null!;
}

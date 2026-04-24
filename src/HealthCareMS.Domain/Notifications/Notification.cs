using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Notifications;

public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    InApp = 3
}

public enum NotificationType
{
    AppointmentBooked = 1,
    AppointmentReminder24Hour = 2,
    AppointmentReminder2Hour = 3,
    LabCriticalValueAlert = 4,
    LabResultReleased = 5
}

public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Skipped = 4
}

public sealed class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }

    public Guid? TenantId { get; set; }

    public NotificationChannel Channel { get; set; }

    public NotificationType Type { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string? Destination { get; set; }

    public string? ReferenceType { get; set; }

    public Guid? ReferenceId { get; set; }

    public DateTimeOffset? ScheduledAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? FailureReason { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public ApplicationUser RecipientUser { get; set; } = null!;

    public Tenant? Tenant { get; set; }
}

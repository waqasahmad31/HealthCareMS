namespace HealthCareMS.Application.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    Guid RecipientUserId,
    string Channel,
    string Type,
    string Status,
    string Subject,
    string Body,
    string? Destination,
    string? ReferenceType,
    Guid? ReferenceId,
    DateTimeOffset? ScheduledAt,
    DateTimeOffset? SentAt,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);

public sealed record NotificationPreferenceResponse(
    Guid UserId,
    bool EmailEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    bool Reminder24HourEnabled,
    bool Reminder2HourEnabled);

public sealed record DeliveryResult(bool IsSuccess, bool IsSkipped, string? FailureReason)
{
    public static readonly DeliveryResult Sent = new(true, false, null);

    public static readonly DeliveryResult Skipped = new(true, true, null);

    public static DeliveryResult Failed(string reason)
    {
        return new(false, false, reason);
    }
}


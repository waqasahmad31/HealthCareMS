namespace HealthCareMS.Blazor.Models;

public sealed record NotificationModel(
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

public sealed record NotificationPreferenceModel(
    Guid UserId,
    bool EmailEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    bool Reminder24HourEnabled,
    bool Reminder2HourEnabled);

public sealed record UpdateNotificationPreferencesModel(
    bool EmailEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    bool Reminder24HourEnabled,
    bool Reminder2HourEnabled);

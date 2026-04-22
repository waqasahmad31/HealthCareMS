namespace HealthCareMS.Application.Notifications;

public sealed record UpdateNotificationPreferencesRequest(
    bool EmailEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    bool Reminder24HourEnabled,
    bool Reminder2HourEnabled);


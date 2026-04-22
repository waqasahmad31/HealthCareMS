using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Notifications;

public interface INotificationService
{
    Task NotifyAppointmentBookedAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task SendAppointmentReminderAsync(Guid appointmentId, string reminderWindow, CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(Guid userId, bool unreadOnly, CancellationToken cancellationToken);

    Task<Result<NotificationResponse>> MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken);

    Task<Result<NotificationPreferenceResponse>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<NotificationPreferenceResponse>> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken);
}

public interface IEmailSender
{
    Task<DeliveryResult> SendAsync(string destination, string subject, string body, CancellationToken cancellationToken);
}

public interface ISmsSender
{
    Task<DeliveryResult> SendAsync(string destination, string body, CancellationToken cancellationToken);
}

public interface IInAppNotificationPublisher
{
    Task PublishAsync(NotificationResponse notification, CancellationToken cancellationToken);
}

public interface IReminderScheduler
{
    string? ScheduleAppointmentReminder(Guid appointmentId, DateTimeOffset sendAt, string reminderWindow);
}

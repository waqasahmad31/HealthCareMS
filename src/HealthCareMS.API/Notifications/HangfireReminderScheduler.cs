using Hangfire;
using HealthCareMS.Application.Notifications;

namespace HealthCareMS.API.Notifications;

public sealed class HangfireReminderScheduler(IBackgroundJobClient backgroundJobClient) : IReminderScheduler
{
    public string? ScheduleAppointmentReminder(Guid appointmentId, DateTimeOffset sendAt, string reminderWindow)
    {
        var delay = sendAt - DateTimeOffset.UtcNow;
        if (delay <= TimeSpan.Zero)
        {
            return backgroundJobClient.Enqueue<INotificationService>(
                service => service.SendAppointmentReminderAsync(appointmentId, reminderWindow, CancellationToken.None));
        }

        return backgroundJobClient.Schedule<INotificationService>(
            service => service.SendAppointmentReminderAsync(appointmentId, reminderWindow, CancellationToken.None),
            delay);
    }
}

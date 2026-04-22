using HealthCareMS.Application.Notifications;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthCareMS.Infrastructure.Notifications;

public sealed class NotificationService(
    HealthCareDbContext dbContext,
    IEmailSender emailSender,
    ISmsSender smsSender,
    IInAppNotificationPublisher inAppPublisher,
    IReminderScheduler? reminderScheduler,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAppointmentBookedAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            logger.LogWarning("Appointment {AppointmentId} not found for booking notification.", appointmentId);
            return;
        }

        var template = NotificationTemplateRenderer.AppointmentBooked(appointment);
        await CreateForAppointmentRecipientAsync(
            appointment,
            appointment.Patient.UserId,
            NotificationType.AppointmentBooked,
            template,
            cancellationToken);

        await CreateInAppAsync(
            appointment.Doctor.UserId,
            appointment.Doctor.TenantId,
            NotificationType.AppointmentBooked,
            template.Subject,
            $"New appointment booked for {appointment.Patient.FirstName} {appointment.Patient.LastName}.",
            "Appointment",
            appointment.Id,
            cancellationToken);

        ScheduleReminders(appointment);
    }

    public async Task SendAppointmentReminderAsync(
        Guid appointmentId,
        string reminderWindow,
        CancellationToken cancellationToken)
    {
        var appointment = await AppointmentQuery()
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            logger.LogWarning("Appointment {AppointmentId} not found for reminder notification.", appointmentId);
            return;
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed or AppointmentStatus.NoShow)
        {
            logger.LogInformation("Skipping appointment reminder for {AppointmentId}; status is {Status}.", appointment.Id, appointment.Status);
            return;
        }

        var notificationType = string.Equals(reminderWindow, "24hr", StringComparison.OrdinalIgnoreCase)
            ? NotificationType.AppointmentReminder24Hour
            : NotificationType.AppointmentReminder2Hour;

        var preferences = await GetOrCreatePreferencesEntityAsync(appointment.Patient.UserId, cancellationToken);
        if (notificationType == NotificationType.AppointmentReminder24Hour && !preferences.Reminder24HourEnabled)
        {
            return;
        }

        if (notificationType == NotificationType.AppointmentReminder2Hour && !preferences.Reminder2HourEnabled)
        {
            return;
        }

        var template = NotificationTemplateRenderer.AppointmentReminder(appointment, reminderWindow);
        await CreateForAppointmentRecipientAsync(
            appointment,
            appointment.Patient.UserId,
            notificationType,
            template,
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(
        Guid userId,
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications
            .Where(x => x.RecipientUserId == userId && x.Channel == NotificationChannel.InApp);

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        var notifications = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return notifications.Select(Map).ToList();
    }

    public async Task<Result<NotificationResponse>> MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId,
            cancellationToken);

        if (notification is null)
        {
            return Result<NotificationResponse>.Failure(new Error("NOTIFICATION_NOT_FOUND", "Notification was not found."));
        }

        notification.IsRead = true;
        notification.ReadAt ??= DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<NotificationResponse>.Success(Map(notification));
    }

    public async Task<Result<NotificationPreferenceResponse>> GetPreferencesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return Result<NotificationPreferenceResponse>.Failure(new Error("NOTIFICATION_USER_NOT_FOUND", "User was not found."));
        }

        var preferences = await GetOrCreatePreferencesEntityAsync(userId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NotificationPreferenceResponse>.Success(Map(preferences));
    }

    public async Task<Result<NotificationPreferenceResponse>> UpdatePreferencesAsync(
        Guid userId,
        UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return Result<NotificationPreferenceResponse>.Failure(new Error("NOTIFICATION_USER_NOT_FOUND", "User was not found."));
        }

        var preferences = await GetOrCreatePreferencesEntityAsync(userId, cancellationToken);
        preferences.EmailEnabled = request.EmailEnabled;
        preferences.SmsEnabled = request.SmsEnabled;
        preferences.InAppEnabled = request.InAppEnabled;
        preferences.Reminder24HourEnabled = request.Reminder24HourEnabled;
        preferences.Reminder2HourEnabled = request.Reminder2HourEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<NotificationPreferenceResponse>.Success(Map(preferences));
    }

    private async Task CreateForAppointmentRecipientAsync(
        Appointment appointment,
        Guid userId,
        NotificationType type,
        NotificationTemplate template,
        CancellationToken cancellationToken)
    {
        var preferences = await GetOrCreatePreferencesEntityAsync(userId, cancellationToken);
        var user = await dbContext.Users.SingleAsync(x => x.Id == userId, cancellationToken);

        if (preferences.InAppEnabled)
        {
            await CreateInAppAsync(
                userId,
                user.TenantId,
                type,
                template.Subject,
                template.Body,
                "Appointment",
                appointment.Id,
                cancellationToken);
        }

        if (preferences.EmailEnabled)
        {
            var emailResult = await emailSender.SendAsync(user.Email, template.Subject, template.Body, cancellationToken);
            dbContext.Notifications.Add(CreateDeliveryNotification(
                userId,
                user.TenantId,
                NotificationChannel.Email,
                type,
                template.Subject,
                template.Body,
                user.Email,
                emailResult,
                appointment.Id));
        }

        var phone = appointment.Patient.UserId == userId ? appointment.Patient.Phone : user.PhoneNumber;
        if (preferences.SmsEnabled)
        {
            var smsResult = await smsSender.SendAsync(phone ?? string.Empty, template.Body, cancellationToken);
            dbContext.Notifications.Add(CreateDeliveryNotification(
                userId,
                user.TenantId,
                NotificationChannel.Sms,
                type,
                template.Subject,
                template.Body,
                phone,
                smsResult,
                appointment.Id));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateInAppAsync(
        Guid userId,
        Guid? tenantId,
        NotificationType type,
        string subject,
        string body,
        string referenceType,
        Guid referenceId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            RecipientUserId = userId,
            TenantId = tenantId,
            Channel = NotificationChannel.InApp,
            Type = type,
            Status = NotificationStatus.Sent,
            Subject = subject,
            Body = body,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            SentAt = DateTimeOffset.UtcNow
        };

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
        await inAppPublisher.PublishAsync(Map(notification), cancellationToken);
    }

    private void ScheduleReminders(Appointment appointment)
    {
        if (reminderScheduler is null)
        {
            return;
        }

        var twentyFourHour = appointment.ScheduledAt.AddHours(-24);
        if (twentyFourHour > DateTimeOffset.UtcNow)
        {
            reminderScheduler.ScheduleAppointmentReminder(appointment.Id, twentyFourHour, "24hr");
        }

        var twoHour = appointment.ScheduledAt.AddHours(-2);
        if (twoHour > DateTimeOffset.UtcNow)
        {
            reminderScheduler.ScheduleAppointmentReminder(appointment.Id, twoHour, "2hr");
        }
    }

    private async Task<NotificationPreference> GetOrCreatePreferencesEntityAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preferences = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (preferences is not null)
        {
            return preferences;
        }

        preferences = new NotificationPreference { UserId = userId };
        dbContext.NotificationPreferences.Add(preferences);
        return preferences;
    }

    private IQueryable<Appointment> AppointmentQuery()
    {
        return dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User);
    }

    private static Notification CreateDeliveryNotification(
        Guid userId,
        Guid? tenantId,
        NotificationChannel channel,
        NotificationType type,
        string subject,
        string body,
        string? destination,
        DeliveryResult result,
        Guid appointmentId)
    {
        return new Notification
        {
            RecipientUserId = userId,
            TenantId = tenantId,
            Channel = channel,
            Type = type,
            Status = result.IsSkipped
                ? NotificationStatus.Skipped
                : result.IsSuccess ? NotificationStatus.Sent : NotificationStatus.Failed,
            Subject = subject,
            Body = body,
            Destination = destination,
            ReferenceType = "Appointment",
            ReferenceId = appointmentId,
            SentAt = result.IsSuccess ? DateTimeOffset.UtcNow : null,
            FailureReason = result.FailureReason
        };
    }

    private static NotificationResponse Map(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.RecipientUserId,
            notification.Channel.ToString(),
            notification.Type.ToString(),
            notification.Status.ToString(),
            notification.Subject,
            notification.Body,
            notification.Destination,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.ScheduledAt,
            notification.SentAt,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt);
    }

    private static NotificationPreferenceResponse Map(NotificationPreference preferences)
    {
        return new NotificationPreferenceResponse(
            preferences.UserId,
            preferences.EmailEnabled,
            preferences.SmsEnabled,
            preferences.InAppEnabled,
            preferences.Reminder24HourEnabled,
            preferences.Reminder2HourEnabled);
    }
}

using HealthCareMS.Application.Notifications;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Notifications;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthCareMS.Tests.Integration;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task NotifyAppointmentBookedAsync_ShouldSendConfirmationAndScheduleReminders()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentAsync(dbContext, DateTimeOffset.UtcNow.AddDays(3));
        var emailSender = new FakeEmailSender();
        var smsSender = new FakeSmsSender();
        var publisher = new FakeInAppPublisher();
        var scheduler = new FakeReminderScheduler();
        var service = CreateService(dbContext, emailSender, smsSender, publisher, scheduler);

        await service.NotifyAppointmentBookedAsync(setup.AppointmentId, CancellationToken.None);

        var notifications = await dbContext.Notifications.ToListAsync();
        Assert.Contains(notifications, x => x.Channel == NotificationChannel.Email && x.Type == NotificationType.AppointmentBooked && x.Status == NotificationStatus.Sent);
        Assert.Contains(notifications, x => x.Channel == NotificationChannel.Sms && x.Type == NotificationType.AppointmentBooked && x.Status == NotificationStatus.Sent);
        Assert.Equal(2, notifications.Count(x => x.Channel == NotificationChannel.InApp));
        Assert.Single(emailSender.Deliveries);
        Assert.Single(smsSender.Deliveries);
        Assert.Equal(2, publisher.Published.Count);
        Assert.Contains(scheduler.Scheduled, x => x.Window == "24hr");
        Assert.Contains(scheduler.Scheduled, x => x.Window == "2hr");
    }

    [Fact]
    public async Task SendAppointmentReminderAsync_ShouldCreateReminderNotifications()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentAsync(dbContext, DateTimeOffset.UtcNow.AddHours(25));
        var emailSender = new FakeEmailSender();
        var smsSender = new FakeSmsSender();
        var publisher = new FakeInAppPublisher();
        var service = CreateService(dbContext, emailSender, smsSender, publisher, new FakeReminderScheduler());

        await service.SendAppointmentReminderAsync(setup.AppointmentId, "24hr", CancellationToken.None);

        var notifications = await dbContext.Notifications.ToListAsync();
        Assert.Contains(notifications, x => x.Type == NotificationType.AppointmentReminder24Hour && x.Channel == NotificationChannel.InApp);
        Assert.Contains(emailSender.Deliveries, x => x.Subject.Contains("24hr", StringComparison.OrdinalIgnoreCase));
        Assert.Single(smsSender.Deliveries);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task PreferencesAsync_ShouldDisableSmsAndAllowMarkAsRead()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentAsync(dbContext, DateTimeOffset.UtcNow.AddDays(2));
        var service = CreateService(dbContext, new FakeEmailSender(), new FakeSmsSender(), new FakeInAppPublisher(), new FakeReminderScheduler());

        var preferences = await service.UpdatePreferencesAsync(
            setup.PatientUserId,
            new UpdateNotificationPreferencesRequest(true, false, true, true, true),
            CancellationToken.None);
        await service.NotifyAppointmentBookedAsync(setup.AppointmentId, CancellationToken.None);

        var patientNotifications = await service.GetForUserAsync(setup.PatientUserId, unreadOnly: true, CancellationToken.None);
        var markRead = await service.MarkAsReadAsync(patientNotifications[0].Id, setup.PatientUserId, CancellationToken.None);

        Assert.True(preferences.IsSuccess);
        Assert.False(preferences.Value.SmsEnabled);
        Assert.DoesNotContain(await dbContext.Notifications.ToListAsync(), x => x.Channel == NotificationChannel.Sms && x.RecipientUserId == setup.PatientUserId);
        Assert.True(markRead.IsSuccess);
        Assert.True(markRead.Value.IsRead);
    }

    private static NotificationService CreateService(
        HealthCareDbContext dbContext,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IInAppNotificationPublisher publisher,
        IReminderScheduler scheduler)
    {
        return new NotificationService(
            dbContext,
            emailSender,
            smsSender,
            publisher,
            scheduler,
            NullLogger<NotificationService>.Instance);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<NotificationSetup> SeedAppointmentAsync(HealthCareDbContext dbContext, DateTimeOffset scheduledAt)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Nadia",
            LastName = "Ali",
            Email = $"patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            PhoneNumber = "+923001111222",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Sara",
            LastName = "Malik",
            Email = $"doctor-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            PhoneNumber = "+923003333444",
            IsActive = true,
            IsEmailVerified = true
        };

        var patient = new Patient
        {
            User = patientUser,
            UserId = patientUser.Id,
            FirstName = patientUser.FirstName,
            LastName = patientUser.LastName,
            DateOfBirth = new DateOnly(1990, 2, 2),
            Gender = Gender.Female,
            Phone = "+923001111222",
            IsActive = true
        };

        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-NOTIFY",
            Specialization = "Family Medicine",
            Qualification = "MBBS",
            Biography = "Clinic doctor",
            City = "Lahore",
            ConsultationFee = 1800m,
            IsVerified = true,
            IsActive = true
        };

        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-{scheduledAt.UtcDateTime:yyyyMMdd}-000001",
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.OnSite,
            Status = AppointmentStatus.Confirmed,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Patient needs appointment notification",
            ConsultationFee = doctor.ConsultationFee
        };

        dbContext.Roles.AddRange(patientRole, doctorRole);
        dbContext.Users.AddRange(patientUser, doctorUser);
        dbContext.Patients.Add(patient);
        dbContext.Doctors.Add(doctor);
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return new NotificationSetup(appointment.Id, patientUser.Id, doctorUser.Id);
    }

    private sealed record NotificationSetup(Guid AppointmentId, Guid PatientUserId, Guid DoctorUserId);

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<(string Destination, string Subject, string Body)> Deliveries { get; } = [];

        public Task<DeliveryResult> SendAsync(string destination, string subject, string body, CancellationToken cancellationToken)
        {
            Deliveries.Add((destination, subject, body));
            return Task.FromResult(DeliveryResult.Sent);
        }
    }

    private sealed class FakeSmsSender : ISmsSender
    {
        public List<(string Destination, string Body)> Deliveries { get; } = [];

        public Task<DeliveryResult> SendAsync(string destination, string body, CancellationToken cancellationToken)
        {
            Deliveries.Add((destination, body));
            return Task.FromResult(DeliveryResult.Sent);
        }
    }

    private sealed class FakeInAppPublisher : IInAppNotificationPublisher
    {
        public List<NotificationResponse> Published { get; } = [];

        public Task PublishAsync(NotificationResponse notification, CancellationToken cancellationToken)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeReminderScheduler : IReminderScheduler
    {
        public List<(Guid AppointmentId, DateTimeOffset SendAt, string Window)> Scheduled { get; } = [];

        public string? ScheduleAppointmentReminder(Guid appointmentId, DateTimeOffset sendAt, string reminderWindow)
        {
            Scheduled.Add((appointmentId, sendAt, reminderWindow));
            return Guid.NewGuid().ToString("N");
        }
    }
}

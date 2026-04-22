using System.Text;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Consultations;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Consultations;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Integration;

public sealed class ConsultationChatServiceTests
{
    [Fact]
    public async Task SendMessageAsync_ShouldStoreRealtimeChatMessageAndReadReceipt()
    {
        await using var dbContext = CreateDbContext();
        var session = await SeedSessionAsync(dbContext, ConsultationSessionStatus.InProgress);
        var service = CreateService(dbContext);

        var sent = await service.SendMessageAsync(
            session.Id,
            new SendChatMessageRequest("Patient", "Ayesha Khan", "Assalam o alaikum doctor"),
            senderUserId: Guid.NewGuid(),
            CancellationToken.None);
        var read = await service.MarkReadAsync(
            session.Id,
            new MarkChatMessagesReadRequest("Doctor"),
            CancellationToken.None);

        Assert.True(sent.IsSuccess);
        Assert.Equal("Text", sent.Value.MessageType);
        Assert.Equal("Patient", sent.Value.SenderType);
        Assert.NotNull(sent.Value.PatientReadAt);
        Assert.Null(sent.Value.DoctorReadAt);

        Assert.True(read.IsSuccess);
        Assert.Single(read.Value);
        Assert.NotNull(read.Value[0].DoctorReadAt);
    }

    [Fact]
    public async Task ChatShouldWorkAfterConsultationSessionCompleted()
    {
        await using var dbContext = CreateDbContext();
        var session = await SeedSessionAsync(dbContext, ConsultationSessionStatus.Completed);
        var service = CreateService(dbContext);

        var result = await service.SendMessageAsync(
            session.Id,
            new SendChatMessageRequest("Doctor", "Dr Sara", "Prescription is available in your portal."),
            senderUserId: Guid.NewGuid(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Doctor", result.Value.SenderType);
        Assert.Equal("Prescription is available in your portal.", result.Value.MessageText);
    }

    [Fact]
    public async Task UploadAttachmentAsync_ShouldStoreFileAndDownloadFromFileService()
    {
        await using var dbContext = CreateDbContext();
        var session = await SeedSessionAsync(dbContext, ConsultationSessionStatus.InProgress);
        var rootPath = TempChatRoot();
        var service = CreateService(dbContext, rootPath);
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("lab-report-placeholder"));

        var uploaded = await service.UploadAttachmentAsync(
            session.Id,
            new UploadChatAttachmentRequest(
                "Doctor",
                "Dr Sara",
                "Report.txt",
                "text/plain",
                content.Length,
                content),
            senderUserId: Guid.NewGuid(),
            CancellationToken.None);
        var downloaded = await service.DownloadAttachmentAsync(uploaded.Value.Id, CancellationToken.None);

        Assert.True(uploaded.IsSuccess);
        Assert.Equal("File", uploaded.Value.MessageType);
        Assert.Equal("Report.txt", uploaded.Value.AttachmentFileName);
        Assert.Contains(uploaded.Value.Id.ToString(), uploaded.Value.DownloadUrl, StringComparison.Ordinal);
        Assert.True(downloaded.IsSuccess);

        string downloadedText;
        using (var reader = new StreamReader(downloaded.Value.Content))
        {
            downloadedText = await reader.ReadToEndAsync();
        }

        Assert.Equal("lab-report-placeholder", downloadedText);

        if (Directory.Exists(rootPath))
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static ConsultationChatService CreateService(HealthCareDbContext dbContext, string? rootPath = null)
    {
        var options = Options.Create(new ChatFileStorageOptions
        {
            RootPath = rootPath ?? TempChatRoot(),
            MaxFileSizeBytes = 10 * 1024 * 1024
        });

        return new ConsultationChatService(dbContext, new LocalChatFileStorage(options), options);
    }

    private static string TempChatRoot()
    {
        return Path.Combine(Path.GetTempPath(), "HealthCareMS-ChatTests", Guid.NewGuid().ToString("N"));
    }

    private static async Task<ConsultationSession> SeedSessionAsync(
        HealthCareDbContext dbContext,
        ConsultationSessionStatus sessionStatus)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Ayesha",
            LastName = "Khan",
            Email = $"chat-patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Sara",
            LastName = "Ahmed",
            Email = $"chat-doctor-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };

        var patient = new Patient
        {
            User = patientUser,
            UserId = patientUser.Id,
            FirstName = patientUser.FirstName,
            LastName = patientUser.LastName,
            DateOfBirth = new DateOnly(1994, 5, 12),
            Gender = Gender.Female,
            Phone = "+923001234567",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = $"PMDC-CHAT-{Guid.NewGuid():N}"[..20],
            Specialization = "Telemedicine",
            Qualification = "MBBS",
            City = "Lahore",
            ConsultationFee = 1800m,
            IsVerified = true,
            IsActive = true
        };

        var scheduledAt = DateTimeOffset.UtcNow.AddDays(1);
        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-CHAT-{Guid.NewGuid():N}"[..24],
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.Online,
            Status = sessionStatus == ConsultationSessionStatus.Completed ? AppointmentStatus.Completed : AppointmentStatus.InProgress,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Chat consultation test appointment",
            ConsultationFee = doctor.ConsultationFee
        };
        var session = new ConsultationSession
        {
            Appointment = appointment,
            AppointmentId = appointment.Id,
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ChannelName = $"consultation-{appointment.Id:N}",
            MeetingLink = $"http://localhost:5157/consultation/waiting-room/{appointment.Id}",
            Status = sessionStatus,
            PatientJoinedAt = scheduledAt,
            DoctorJoinedAt = scheduledAt,
            StartedAt = scheduledAt,
            LastTokenIssuedAt = scheduledAt
        };

        dbContext.Roles.AddRange(patientRole, doctorRole);
        dbContext.Users.AddRange(patientUser, doctorUser);
        dbContext.Patients.Add(patient);
        dbContext.Doctors.Add(doctor);
        dbContext.Appointments.Add(appointment);
        dbContext.ConsultationSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return session;
    }
}

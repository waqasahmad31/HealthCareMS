using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Appointments;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Appointments;
using HealthCareMS.Infrastructure.Consultations;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Integration;

public sealed class ConsultationSessionServiceTests
{
    [Fact]
    public async Task ConfirmAsync_ShouldEmbedMeetingLink_ForOnlineAppointment()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedOnlineAppointmentAsync(dbContext, AppointmentStatus.Pending);
        var service = new AppointmentService(dbContext, new FakeCurrentUser(Guid.NewGuid()));

        var result = await service.ConfirmAsync(appointment.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value.Status);
        Assert.Equal("Online", result.Value.Type);
        Assert.Equal($"/consultation/waiting-room/{appointment.Id}", result.Value.MeetingLink);
    }

    [Fact]
    public async Task StartAsync_ShouldCreateSessionAndAgoraTokenJoinFlow()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedOnlineAppointmentAsync(dbContext, AppointmentStatus.Confirmed);
        var service = CreateService(dbContext);

        var started = await service.StartAsync(new StartConsultationSessionRequest(appointment.Id), CancellationToken.None);
        var patientJoin = await service.JoinAsync(
            started.Value.Id,
            new JoinConsultationSessionRequest("Patient"),
            CancellationToken.None);
        var doctorJoin = await service.JoinAsync(
            started.Value.Id,
            new JoinConsultationSessionRequest("Doctor"),
            CancellationToken.None);

        var updatedAppointment = await dbContext.Appointments.SingleAsync(x => x.Id == appointment.Id);

        Assert.True(started.IsSuccess);
        Assert.StartsWith("consultation-", started.Value.ChannelName, StringComparison.Ordinal);
        Assert.Equal($"http://localhost:5157/consultation/waiting-room/{appointment.Id}", started.Value.MeetingLink);
        Assert.Equal(started.Value.MeetingLink, updatedAppointment.MeetingLink);

        Assert.True(patientJoin.IsSuccess);
        Assert.Equal("Patient", patientJoin.Value.ParticipantType);
        Assert.Equal("test-agora-app", patientJoin.Value.AgoraAppId);
        Assert.False(string.IsNullOrWhiteSpace(patientJoin.Value.Token));
        Assert.True(patientJoin.Value.PatientIsOnline);

        Assert.True(doctorJoin.IsSuccess);
        Assert.Equal("InProgress", doctorJoin.Value.Session.Status);
        Assert.True(doctorJoin.Value.DoctorIsOnline);
        Assert.Equal(AppointmentStatus.InProgress, updatedAppointment.Status);
    }

    [Fact]
    public async Task StartAsync_ShouldRejectNonOnlineOrUnconfirmedAppointment()
    {
        await using var dbContext = CreateDbContext();
        var onSite = await SeedAppointmentAsync(dbContext, AppointmentType.OnSite, AppointmentStatus.Confirmed);
        var pendingOnline = await SeedAppointmentAsync(dbContext, AppointmentType.Online, AppointmentStatus.Pending);
        var service = CreateService(dbContext);

        var onSiteResult = await service.StartAsync(new StartConsultationSessionRequest(onSite.Id), CancellationToken.None);
        var pendingResult = await service.StartAsync(new StartConsultationSessionRequest(pendingOnline.Id), CancellationToken.None);

        Assert.True(onSiteResult.IsFailure);
        Assert.Equal("SESSION_APPOINTMENT_TYPE_INVALID", onSiteResult.Error.Code);
        Assert.True(pendingResult.IsFailure);
        Assert.Equal("SESSION_APPOINTMENT_NOT_CONFIRMED", pendingResult.Error.Code);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static ConsultationSessionService CreateService(HealthCareDbContext dbContext)
    {
        return new ConsultationSessionService(
            dbContext,
            Options.Create(new ConsultationSessionOptions
            {
                AppId = "test-agora-app",
                AppCertificate = "test-agora-secret",
                ClientBaseUrl = "http://localhost:5157",
                TokenExpiryMinutes = 60
            }));
    }

    private static Task<Appointment> SeedOnlineAppointmentAsync(
        HealthCareDbContext dbContext,
        AppointmentStatus status)
    {
        return SeedAppointmentAsync(dbContext, AppointmentType.Online, status);
    }

    private static async Task<Appointment> SeedAppointmentAsync(
        HealthCareDbContext dbContext,
        AppointmentType type,
        AppointmentStatus status)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Maham",
            LastName = "Noor",
            Email = $"maham-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Danish",
            LastName = "Iqbal",
            Email = $"dr-danish-{Guid.NewGuid():N}@example.com",
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
            DateOfBirth = new DateOnly(1991, 3, 11),
            Gender = Gender.Female,
            Phone = "+923002222222",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = $"PMDC-VIDEO-{Guid.NewGuid():N}"[..20],
            Specialization = "Family Medicine",
            Qualification = "MBBS",
            City = "Karachi",
            ConsultationFee = 2000m,
            IsVerified = true,
            IsActive = true
        };

        var scheduledAt = DateTimeOffset.UtcNow.AddDays(1);
        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-VIDEO-{Guid.NewGuid():N}"[..24],
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = type,
            Status = status,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Video consultation test appointment",
            ConsultationFee = doctor.ConsultationFee
        };

        dbContext.Roles.AddRange(patientRole, doctorRole);
        dbContext.Users.AddRange(patientUser, doctorUser);
        dbContext.Patients.Add(patient);
        dbContext.Doctors.Add(doctor);
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return appointment;
    }

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId => null;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsSuperAdmin => false;

        public IReadOnlyCollection<string> Permissions { get; } = [PermissionKeys.Appointment.Confirm];
    }
}

using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Queues;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Queues;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class QueueServiceTests
{
    [Fact]
    public async Task RegisterWalkInAsync_ShouldCreateCheckedInQueueEntry_WithEstimatedWait()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedQueueSetupAsync(dbContext);
        var service = new QueueService(dbContext, new FakeCurrentUser(Guid.NewGuid()));

        var result = await service.RegisterWalkInAsync(
            new WalkInRegistrationRequest(
                setup.PatientId,
                setup.DoctorId,
                "Walk-in patient has fever and cough",
                "Normal",
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.QueueNumber);
        Assert.Equal("Confirmed", result.Value.Status);
        Assert.Equal(1, result.Value.Position);
        Assert.Equal(0, result.Value.EstimatedWaitMinutes);
        Assert.NotNull(result.Value.CheckedInAt);
    }

    [Fact]
    public async Task CheckInAsync_ShouldAssignQueueNumberAndReturnPatientStatus()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedQueueSetupAsync(dbContext);
        var appointment = await SeedAppointmentAsync(dbContext, setup.PatientId, setup.DoctorId, queueNumber: null);
        var service = new QueueService(dbContext, new FakeCurrentUser(Guid.NewGuid()));

        var checkIn = await service.CheckInAsync(appointment.Id, CancellationToken.None);
        var status = await service.GetPatientStatusAsync(appointment.Id, CancellationToken.None);

        Assert.True(checkIn.IsSuccess);
        Assert.Equal(1, checkIn.Value.QueueNumber);
        Assert.Equal("Confirmed", checkIn.Value.Status);
        Assert.True(status.IsSuccess);
        Assert.Equal(1, status.Value.QueueNumber);
        Assert.Equal(1, status.Value.Position);
    }

    [Fact]
    public async Task CallNextAsync_ShouldMarkFirstWaitingPatientInProgress()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedQueueSetupAsync(dbContext);
        var secondPatient = await SeedPatientAsync(dbContext, "queue.second@example.com");
        var first = await SeedAppointmentAsync(dbContext, setup.PatientId, setup.DoctorId, queueNumber: 1);
        await SeedAppointmentAsync(dbContext, secondPatient.PatientId, setup.DoctorId, queueNumber: 2);
        var service = new QueueService(dbContext, new FakeCurrentUser(Guid.NewGuid()));
        var date = DateOnly.FromDateTime(first.ScheduledAt.UtcDateTime);

        var next = await service.CallNextAsync(setup.DoctorId, date, CancellationToken.None);
        var board = await service.GetBoardAsync(setup.DoctorId, date, CancellationToken.None);

        Assert.True(next.IsSuccess);
        Assert.Equal(first.Id, next.Value.AppointmentId);
        Assert.Equal("InProgress", next.Value.Status);
        Assert.True(board.IsSuccess);
        Assert.Equal(1, board.Value.InProgressCount);
        Assert.Equal(1, board.Value.WaitingCount);
        Assert.Equal(first.Id, board.Value.NextPatient?.AppointmentId);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<QueueSetup> SeedQueueSetupAsync(HealthCareDbContext dbContext)
    {
        var patient = await SeedPatientAsync(dbContext, "queue.patient@example.com");
        var doctorRole = new Role { Name = "Doctor", IsSystemRole = true };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Sara",
            LastName = "Malik",
            Email = "dr.sara@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };

        var today = DateTimeOffset.UtcNow.UtcDateTime.DayOfWeek;
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-QUEUE",
            Specialization = "Family Medicine",
            Qualification = "MBBS",
            Biography = "Clinic doctor",
            City = "Lahore",
            ConsultationFee = 1800m,
            IsVerified = true,
            IsActive = true
        };

        doctor.Schedules.Add(new DoctorSchedule
        {
            Doctor = doctor,
            DoctorId = doctor.Id,
            DayOfWeek = today,
            StartTime = TimeOnly.MinValue,
            EndTime = new TimeOnly(23, 59),
            SlotDurationMinutes = 15,
            IsOnlineAvailable = false,
            IsOnSiteAvailable = true
        });

        dbContext.Roles.Add(doctorRole);
        dbContext.Users.Add(doctorUser);
        dbContext.Doctors.Add(doctor);
        await dbContext.SaveChangesAsync();

        return new QueueSetup(patient.PatientId, doctor.Id);
    }

    private static async Task<PatientSetup> SeedPatientAsync(HealthCareDbContext dbContext, string email)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Nadia",
            LastName = "Ali",
            Email = email,
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
            DateOfBirth = new DateOnly(1990, 2, 2),
            Gender = Gender.Female,
            Phone = "+923001111222",
            IsActive = true
        };

        dbContext.Roles.Add(patientRole);
        dbContext.Users.Add(patientUser);
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        return new PatientSetup(patient.Id);
    }

    private static async Task<Appointment> SeedAppointmentAsync(
        HealthCareDbContext dbContext,
        Guid patientId,
        Guid doctorId,
        int? queueNumber)
    {
        var patient = await dbContext.Patients.Include(x => x.User).SingleAsync(x => x.Id == patientId);
        var doctor = await dbContext.Doctors.Include(x => x.User).SingleAsync(x => x.Id == doctorId);
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(45 + (queueNumber ?? 0));
        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-{scheduledAt.UtcDateTime:yyyyMMdd}-{Guid.NewGuid():N}"[..22],
            PatientId = patientId,
            Patient = patient,
            DoctorId = doctorId,
            Doctor = doctor,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.OnSite,
            Status = AppointmentStatus.Pending,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Patient waiting for onsite consultation",
            ConsultationFee = doctor.ConsultationFee,
            QueueNumber = queueNumber,
            CheckedInAt = queueNumber.HasValue ? DateTimeOffset.UtcNow.AddMinutes(queueNumber.Value) : null
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return appointment;
    }

    private sealed record QueueSetup(Guid PatientId, Guid DoctorId);

    private sealed record PatientSetup(Guid PatientId);

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId => null;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsSuperAdmin => false;

        public IReadOnlyCollection<string> Permissions { get; } = [PermissionKeys.Appointment.Book];
    }
}

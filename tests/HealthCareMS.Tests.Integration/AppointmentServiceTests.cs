using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Appointments;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class AppointmentServiceTests
{
    [Fact]
    public async Task BookAsync_ShouldCreatePendingOnSiteAppointment_WithQueueNumber()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var service = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));

        var result = await service.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                setup.ScheduledAt,
                "OnSite",
                30,
                "Patient has recurring chest pain",
                "Normal",
                "Prefers morning visit"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal("OnSite", result.Value.Type);
        Assert.Equal(1, result.Value.QueueNumber);
        Assert.StartsWith("APT-", result.Value.AppointmentNumber, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BookAsync_ShouldAcceptLocalOffsetScheduleAndNormalizeToUtc()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var service = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));
        var localOffsetScheduledAt = new DateTimeOffset(
            setup.ScheduledAt.Year,
            setup.ScheduledAt.Month,
            setup.ScheduledAt.Day,
            9,
            0,
            0,
            TimeSpan.FromHours(5));

        var result = await service.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                localOffsetScheduledAt,
                "Online",
                30,
                "Patient needs online consultation",
                "Normal",
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TimeSpan.Zero, result.Value.ScheduledAt.Offset);
        Assert.Equal(localOffsetScheduledAt.UtcDateTime, result.Value.ScheduledAt.UtcDateTime);
    }

    [Fact]
    public async Task BookAsync_ShouldAllowSlotReturnedByAvailability_ForLocalOffsetBooking()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var appointmentService = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));
        var doctorService = new HealthCareMS.Infrastructure.Doctors.DoctorService(dbContext, new FakePasswordHasher(), new TestDistributedQueryCache());
        var localDate = DateOnly.FromDateTime(setup.ScheduledAt.UtcDateTime);

        var availableSlots = await doctorService.GetAvailableSlotsAsync(setup.DoctorId, localDate, "OnSite", CancellationToken.None, utcOffsetMinutes: 300);

        Assert.True(availableSlots.IsSuccess);
        Assert.NotEmpty(availableSlots.Value);

        var firstSlot = availableSlots.Value[0];
        var localOffsetScheduledAt = new DateTimeOffset(
            localDate.Year,
            localDate.Month,
            localDate.Day,
            firstSlot.StartTime.Hour,
            firstSlot.StartTime.Minute,
            0,
            TimeSpan.FromHours(5));

        var result = await appointmentService.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                localOffsetScheduledAt,
                firstSlot.AppointmentType,
                30,
                "Patient booked from availability inventory",
                "Normal",
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status);
        Assert.Equal(localOffsetScheduledAt.UtcDateTime, result.Value.ScheduledAt.UtcDateTime);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ShouldExcludePreviouslyBookedLocalOffsetSlot()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var appointmentService = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));
        var doctorService = new HealthCareMS.Infrastructure.Doctors.DoctorService(dbContext, new FakePasswordHasher(), new TestDistributedQueryCache());
        var localOffsetScheduledAt = new DateTimeOffset(
            setup.ScheduledAt.Year,
            setup.ScheduledAt.Month,
            setup.ScheduledAt.Day,
            9,
            0,
            0,
            TimeSpan.FromHours(5));
        var localDate = new DateOnly(localOffsetScheduledAt.Year, localOffsetScheduledAt.Month, localOffsetScheduledAt.Day);

        var booked = await appointmentService.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                localOffsetScheduledAt,
                "OnSite",
                30,
                "Patient booked from doctor availability workspace",
                "Normal",
                null),
            CancellationToken.None);

        var availableSlots = await doctorService.GetAvailableSlotsAsync(setup.DoctorId, localDate, "OnSite", CancellationToken.None, utcOffsetMinutes: 300);

        Assert.True(booked.IsSuccess);
        Assert.True(availableSlots.IsSuccess);
        Assert.DoesNotContain(availableSlots.Value, slot => slot.StartTime == new TimeOnly(9, 0) && slot.EndTime == new TimeOnly(9, 30));
        Assert.Equal(3, availableSlots.Value.Count);
    }

    [Fact]
    public async Task BookAsync_ShouldRejectOverlappingSlot_ForSameDoctor()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var secondPatient = await SeedPatientAsync(dbContext, "second.patient@example.com");
        var service = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));

        var first = await service.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                setup.ScheduledAt,
                "Online",
                30,
                "Patient needs a follow up review",
                null,
                null),
            CancellationToken.None);

        var conflict = await service.BookAsync(
            new BookAppointmentRequest(
                secondPatient.PatientId,
                setup.DoctorId,
                setup.ScheduledAt.AddMinutes(15),
                "Online",
                30,
                "Patient has new consultation concern",
                null,
                null),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(conflict.IsFailure);
        Assert.Equal("APT_SLOT_CONFLICT", conflict.Error.Code);
    }

    [Fact]
    public async Task AppointmentLifecycleAsync_ShouldConfirmRescheduleCompleteAndCancel()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAppointmentSetupAsync(dbContext);
        var service = new AppointmentService(dbContext, new FakeCurrentUser(setup.PatientUserId));

        var booked = await service.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                setup.ScheduledAt,
                "OnSite",
                30,
                "Patient needs doctor consultation",
                "High",
                null),
            CancellationToken.None);

        var confirmed = await service.ConfirmAsync(booked.Value.Id, CancellationToken.None);
        var rescheduled = await service.RescheduleAsync(
            booked.Value.Id,
            new RescheduleAppointmentRequest(setup.ScheduledAt.AddHours(1), 30, "Patient requested a later slot"),
            CancellationToken.None);
        var completed = await service.CompleteAsync(
            booked.Value.Id,
            new CompleteAppointmentRequest("Stable condition", "No acute finding", null),
            CancellationToken.None);

        var cancelBooked = await service.BookAsync(
            new BookAppointmentRequest(
                setup.PatientId,
                setup.DoctorId,
                setup.ScheduledAt.AddMinutes(90),
                "Online",
                30,
                "Patient needs another consultation",
                null,
                null),
            CancellationToken.None);
        var cancelled = await service.CancelAsync(
            cancelBooked.Value.Id,
            new CancelAppointmentRequest("Patient is unavailable", "Patient"),
            CancellationToken.None);

        Assert.True(booked.IsSuccess);
        Assert.True(confirmed.IsSuccess);
        Assert.Equal("Confirmed", confirmed.Value.Status);
        Assert.True(rescheduled.IsSuccess);
        Assert.Equal("Pending", rescheduled.Value.Status);
        Assert.Equal(setup.ScheduledAt.AddHours(1), rescheduled.Value.ScheduledAt);
        Assert.True(completed.IsSuccess);
        Assert.Equal("Completed", completed.Value.Status);
        Assert.True(cancelled.IsSuccess);
        Assert.Equal("Cancelled", cancelled.Value.Status);
        Assert.Equal("Patient is unavailable", cancelled.Value.CancellationReason);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<AppointmentSetup> SeedAppointmentSetupAsync(HealthCareDbContext dbContext)
    {
        var patient = await SeedPatientAsync(dbContext, "patient@example.com");
        var doctorRole = new Role { Name = "Doctor", IsSystemRole = true };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Hamza",
            LastName = "Raza",
            Email = "dr.hamza@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };

        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-12345",
            Specialization = "Cardiology",
            Qualification = "MBBS, FCPS",
            Biography = "Consultant cardiologist",
            City = "Karachi",
            ConsultationFee = 2500m,
            IsVerified = true,
            IsActive = true
        };

        var scheduledAt = Next(DayOfWeek.Monday).ToDateTime(new TimeOnly(9, 0));
        doctor.Schedules.Add(new DoctorSchedule
        {
            Doctor = doctor,
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            SlotDurationMinutes = 30,
            IsOnlineAvailable = true,
            IsOnSiteAvailable = true
        });

        dbContext.Roles.Add(doctorRole);
        dbContext.Users.Add(doctorUser);
        dbContext.Doctors.Add(doctor);
        await dbContext.SaveChangesAsync();

        return new AppointmentSetup(
            patient.PatientId,
            patient.PatientUserId,
            doctor.Id,
            new DateTimeOffset(scheduledAt, TimeSpan.Zero));
    }

    private static async Task<PatientSetup> SeedPatientAsync(HealthCareDbContext dbContext, string email)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Ayesha",
            LastName = "Khan",
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
            DateOfBirth = new DateOnly(1994, 5, 12),
            Gender = Gender.Female,
            BloodGroup = "B+",
            Phone = "+923001234567",
            AddressCity = "Lahore",
            AddressProvince = "Punjab",
            IsActive = true
        };

        dbContext.Roles.Add(patientRole);
        dbContext.Users.Add(patientUser);
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        return new PatientSetup(patient.Id, patientUser.Id);
    }

    private static DateOnly Next(DayOfWeek dayOfWeek)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysToAdd = ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7;
        if (daysToAdd == 0)
        {
            daysToAdd = 7;
        }

        return date.AddDays(daysToAdd);
    }

    private sealed record AppointmentSetup(
        Guid PatientId,
        Guid PatientUserId,
        Guid DoctorId,
        DateTimeOffset ScheduledAt);

    private sealed record PatientSetup(Guid PatientId, Guid PatientUserId);

    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId => null;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsSuperAdmin => false;

        public IReadOnlyCollection<string> Permissions { get; } = [PermissionKeys.Appointment.Book];
    }

    private sealed class FakePasswordHasher : HealthCareMS.Application.Abstractions.Authentication.IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string password, string passwordHash) => password == passwordHash;
    }
}

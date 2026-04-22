using HealthCareMS.Application.Admin;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Admin;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class AdminOperationsServiceTests
{
    [Fact]
    public async Task GetAppointmentOverviewAsync_ShouldReturnFilteredCountsRowsAndRevenue()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAdminSetupAsync(dbContext);
        var service = new AdminOperationsService(dbContext);

        var result = await service.GetAppointmentOverviewAsync(
            patientId: null,
            doctorId: setup.DoctorId,
            status: null,
            date: setup.Today,
            pageNumber: 1,
            pageSize: 20,
            CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.PendingCount);
        Assert.Equal(1, result.CompletedCount);
        Assert.Equal(1, result.CancelledCount);
        Assert.Equal(3000m, result.CompletedFeeTotal);
        Assert.Equal(3, result.Appointments.Count);
        Assert.Contains(result.Appointments, x => x.AppointmentNumber == "APT-ADMIN-COMPLETE");
    }

    [Fact]
    public async Task DoctorManagementAsync_ShouldFilterAndUpdateVerificationAndActiveState()
    {
        await using var dbContext = CreateDbContext();
        var setup = await SeedAdminSetupAsync(dbContext);
        var service = new AdminOperationsService(dbContext);

        var before = await service.GetDoctorManagementAsync(null, "Lahore", isVerified: false, isActive: true, CancellationToken.None);
        var updated = await service.SetDoctorStatusAsync(
            setup.DoctorId,
            new UpdateDoctorAdminStatusRequest(IsVerified: true, IsActive: false),
            CancellationToken.None);
        var after = await service.GetDoctorManagementAsync(null, null, isVerified: true, isActive: false, CancellationToken.None);

        Assert.Equal(1, before.TotalCount);
        Assert.True(updated.IsSuccess);
        Assert.True(updated.Value.IsVerified);
        Assert.False(updated.Value.IsActive);
        Assert.Single(after.Doctors);
        Assert.Equal(setup.DoctorId, after.Doctors[0].Id);
    }

    [Fact]
    public async Task SystemConfigurationAsync_ShouldSeedDefaultsAndValidateSettingUpdates()
    {
        await using var dbContext = CreateDbContext();
        var service = new AdminOperationsService(dbContext);

        var configuration = await service.GetSystemConfigurationAsync(CancellationToken.None);
        var updated = await service.UpdateSystemSettingAsync(
            "Security.MaintenanceMode",
            new UpdateSystemSettingRequest("true"),
            CancellationToken.None);
        var invalid = await service.UpdateSystemSettingAsync(
            "Security.MaintenanceMode",
            new UpdateSystemSettingRequest("not-bool"),
            CancellationToken.None);

        Assert.Contains(configuration.Settings, x => x.SettingKey == "Platform.DefaultCurrency" && x.Value == "PKR");
        Assert.True(updated.IsSuccess);
        Assert.Equal("true", updated.Value.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("ADMIN_SETTING_VALUE_INVALID", invalid.Error.Code);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<AdminSetup> SeedAdminSetupAsync(HealthCareDbContext dbContext)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Nida",
            LastName = "Ali",
            Email = $"nida-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Usman",
            LastName = "Riaz",
            Email = $"dr-usman-{Guid.NewGuid():N}@example.com",
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
            DateOfBirth = new DateOnly(1993, 7, 20),
            Gender = Gender.Female,
            Phone = "+923001111111",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-ADMIN",
            Specialization = "Dermatology",
            Qualification = "MBBS, FCPS",
            City = "Lahore",
            ConsultationFee = 3000m,
            IsVerified = false,
            IsActive = true
        };

        doctor.Schedules.Add(new DoctorSchedule
        {
            Doctor = doctor,
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            SlotDurationMinutes = 30,
            IsOnlineAvailable = true,
            IsOnSiteAvailable = true
        });

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1));
        var start = new DateTimeOffset(today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero);
        var pending = CreateAppointment("APT-ADMIN-PENDING", patient, doctor, start, AppointmentStatus.Pending);
        var completed = CreateAppointment("APT-ADMIN-COMPLETE", patient, doctor, start.AddMinutes(30), AppointmentStatus.Completed);
        var cancelled = CreateAppointment("APT-ADMIN-CANCELLED", patient, doctor, start.AddMinutes(60), AppointmentStatus.Cancelled);

        dbContext.Roles.AddRange(patientRole, doctorRole);
        dbContext.Users.AddRange(patientUser, doctorUser);
        dbContext.Patients.Add(patient);
        dbContext.Doctors.Add(doctor);
        dbContext.Appointments.AddRange(pending, completed, cancelled);
        await dbContext.SaveChangesAsync();

        return new AdminSetup(today, doctor.Id);
    }

    private static Appointment CreateAppointment(
        string appointmentNumber,
        Patient patient,
        Doctor doctor,
        DateTimeOffset scheduledAt,
        AppointmentStatus status)
    {
        return new Appointment
        {
            AppointmentNumber = appointmentNumber,
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.OnSite,
            Status = status,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Admin overview test appointment",
            ConsultationFee = doctor.ConsultationFee,
            PaymentStatus = PaymentStatus.Pending,
            QueueNumber = 1
        };
    }

    private sealed record AdminSetup(DateOnly Today, Guid DoctorId);
}

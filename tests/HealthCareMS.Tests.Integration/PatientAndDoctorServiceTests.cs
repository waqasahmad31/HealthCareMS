using HealthCareMS.Application.Doctors;
using HealthCareMS.Application.Patients;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Doctors;
using HealthCareMS.Infrastructure.Patients;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class PatientAndDoctorServiceTests
{
    [Fact]
    public async Task RegisterPatientAsync_ShouldCreatePatientAndMedicalHistory()
    {
        await using var dbContext = CreateDbContext();
        await SeedRoleAsync(dbContext, "Patient");
        var service = new PatientService(dbContext, new Pbkdf2PasswordHasher());

        var result = await service.RegisterAsync(
            new RegisterPatientRequest(
                "Ayesha",
                "Khan",
                "ayesha@example.com",
                "StrongPass123",
                "3520212345678",
                new DateOnly(1994, 5, 12),
                "Female",
                "B+",
                "+923001234567",
                null,
                "Street 4",
                "Lahore",
                "Punjab",
                "54000",
                "Ali Khan",
                "+923009876543",
                "Brother"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.NotNull(result.Value.MedicalHistory);
        Assert.Equal("ayesha@example.com", result.Value.Email);
    }

    [Fact]
    public async Task DoctorScheduleAsync_ShouldReturnAvailableSlotsForRequestedDate()
    {
        await using var dbContext = CreateDbContext();
        await SeedRoleAsync(dbContext, "Doctor");
        var service = new DoctorService(dbContext, new Pbkdf2PasswordHasher());

        var createResult = await service.CreateProfileAsync(
            new CreateDoctorProfileRequest(
                null,
                "Hamza",
                "Raza",
                "dr.hamza@example.com",
                "StrongPass123",
                "+923001111111",
                "PMDC-12345",
                "Cardiology",
                "MBBS, FCPS",
                "Consultant cardiologist",
                "Karachi",
                2500m),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);

        var date = Next(DayOfWeek.Monday);
        var scheduleResult = await service.SetScheduleAsync(
            createResult.Value.Id,
            new SetDoctorScheduleRequest(
            [
                new DoctorScheduleSlotRequest(
                    "Monday",
                    new TimeOnly(9, 0),
                    new TimeOnly(10, 0),
                    30,
                    true,
                    true)
            ]),
            CancellationToken.None);

        var slotsResult = await service.GetAvailableSlotsAsync(createResult.Value.Id, date, "Online", CancellationToken.None);

        Assert.True(scheduleResult.IsSuccess);
        Assert.True(slotsResult.IsSuccess);
        Assert.Equal(2, slotsResult.Value.Count);
        Assert.Equal(new TimeOnly(9, 0), slotsResult.Value[0].StartTime);
        Assert.Equal(new TimeOnly(10, 0), slotsResult.Value[1].EndTime);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task SeedRoleAsync(HealthCareDbContext dbContext, string roleName)
    {
        dbContext.Roles.Add(new Role
        {
            Name = roleName,
            IsSystemRole = true
        });

        await dbContext.SaveChangesAsync();
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
}

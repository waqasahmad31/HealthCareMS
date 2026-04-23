using System.Text;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Labs;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Consultations;
using HealthCareMS.Infrastructure.Labs;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Pharmacy;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Tests.Integration;

public sealed class LabAndPharmacyServiceTests
{
    [Fact]
    public async Task CreateConsultationLabOrderAsync_ShouldCreateBookingAndSummaryPdf()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var labService = new LabService(dbContext);
        var consultationService = new ConsultationService(
            dbContext,
            new QuestPdfPrescriptionDocumentService(),
            null,
            new QuestPdfConsultationSummaryDocumentService());

        var complete = await consultationService.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Diabetes follow-up",
                "E11.9",
                "Order labs and continue monitoring.",
                null,
                [new PrescriptionItemRequest("Glucophage", "Metformin", "500mg", "Oral", "1 tablet", "BID", 30, 60, "After meals", true)]),
            CancellationToken.None);
        var tests = await labService.SearchTestsAsync("hba", CancellationToken.None);
        var order = await labService.CreateConsultationLabOrderAsync(
            appointment.Id,
            new CreateConsultationLabOrderRequest([tests.Single().Id], "OnSite", null, null, "Doctor ordered from consultation"),
            CancellationToken.None);
        var summary = await consultationService.GetSummaryAsync(appointment.Id, CancellationToken.None);
        var pdf = await consultationService.GenerateSummaryPdfAsync(appointment.Id, CancellationToken.None);

        Assert.True(complete.IsSuccess);
        Assert.True(order.IsSuccess);
        Assert.StartsWith("LAB-", order.Value.BookingNumber, StringComparison.Ordinal);
        Assert.Equal("HBA1C", order.Value.Items[0].TestCode);
        Assert.True(summary.IsSuccess);
        Assert.Single(summary.Value.LabOrders);
        Assert.NotNull(summary.Value.Prescription);
        Assert.True(pdf.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.Value.Content, 0, 4), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PharmacyService_ShouldCreateMedicineBatchAndImportCsv()
    {
        await using var dbContext = CreateDbContext();
        var service = new PharmacyService(dbContext);

        var medicine = await service.CreateMedicineAsync(
            new CreateMedicineRequest(
                null,
                "Paracetamol",
                "Panadol",
                "Tablet",
                "500mg",
                "DRAP-PAN-500",
                "GSK Pakistan",
                25m,
                15m,
                false,
                20,
                null),
            CancellationToken.None);
        var batch = await service.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(
                null,
                null,
                "BATCH-001",
                DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2)),
                150,
                15m),
            CancellationToken.None);
        var csv = """
            GenericName,BrandName,DosageForm,Strength,DrapRegistrationNumber,Manufacturer,UnitPrice,UnitCostPrice,IsControlled,ReorderLevel,Barcode
            Ibuprofen,Brufen,Tablet,400mg,DRAP-BRU-400,Abbott,35,22,false,25,CSV-BRUFEN-400
            """;
        var imported = await service.ImportMedicinesCsvAsync(new ImportMedicineCsvRequest(null, csv), CancellationToken.None);
        var search = await service.SearchMedicinesAsync("brufen", CancellationToken.None);

        Assert.True(medicine.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(medicine.Value.Barcode));
        Assert.True(batch.IsSuccess);
        Assert.Equal(150, batch.Value.QuantityOnHand);
        Assert.True(imported.IsSuccess);
        Assert.Equal(1, imported.Value.ImportedCount);
        Assert.Contains(search, x => x.BrandName == "Brufen" && x.Barcode == "CSV-BRUFEN-400");
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<Appointment> SeedAppointmentAsync(HealthCareDbContext dbContext)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Sana",
            LastName = "Malik",
            Email = $"lab-patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Adeel",
            LastName = "Ahmed",
            Email = $"lab-doctor-{Guid.NewGuid():N}@example.com",
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
            DateOfBirth = new DateOnly(1988, 8, 8),
            Gender = Gender.Female,
            Phone = "+923001234567",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-LAB-ORDER",
            Specialization = "Internal Medicine",
            Qualification = "MBBS",
            Biography = "Consultant",
            City = "Lahore",
            ConsultationFee = 2500m,
            IsVerified = true,
            IsActive = true
        };
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(-20);
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
            Type = AppointmentType.Online,
            Status = AppointmentStatus.InProgress,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Diabetes follow-up consultation",
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
}

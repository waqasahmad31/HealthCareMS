using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Consultations;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace HealthCareMS.Tests.Integration;

public sealed class ConsultationServiceTests
{
    [Fact]
    public async Task CompleteAsync_ShouldCompleteAppointmentAndCreateMultiDrugPrescription()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var service = new ConsultationService(dbContext);

        var result = await service.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Acute upper respiratory infection",
                "J06.9",
                "Patient is stable and hydrated.",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                [
                    new PrescriptionItemRequest("Paracetamol", "Acetaminophen", "500mg", "Oral", "1 tablet", "TID", 5, 15, "After meals", true),
                    new PrescriptionItemRequest("Cetirizine", "Cetirizine", "10mg", "Oral", "1 tablet", "OD", 5, 5, "At night", true)
                ]),
            CancellationToken.None);

        var prescription = await service.GetPrescriptionByAppointmentAsync(appointment.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value.Status);
        Assert.Equal("J06.9", result.Value.Icd10Code);
        Assert.Equal(2, result.Value.Prescription?.Items.Count);
        Assert.StartsWith("RX-", result.Value.Prescription?.PrescriptionNumber, StringComparison.Ordinal);
        Assert.True(prescription.IsSuccess);
        Assert.Equal(appointment.Id, prescription.Value.AppointmentId);
        Assert.Equal("Paracetamol", prescription.Value.Items[0].MedicineName);
        Assert.NotEmpty(result.Value.Prescription?.VerificationCode ?? string.Empty);
        Assert.NotEmpty(result.Value.Prescription?.DigitalSignature ?? string.Empty);
    }

    [Fact]
    public async Task CompleteAsync_ShouldRejectUnknownIcd10Code()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var service = new ConsultationService(dbContext);

        var result = await service.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Unknown code diagnosis",
                "ZZZ",
                null,
                null,
                [new PrescriptionItemRequest("Paracetamol", null, "500mg", "Oral", "1 tablet", "BID", 3, 6, null, true)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ICD10_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public async Task SearchIcd10Async_ShouldReturnMatchingCodes()
    {
        await using var dbContext = CreateDbContext();
        var service = new ConsultationService(dbContext);

        var results = await service.SearchIcd10Async("hypertension", CancellationToken.None);

        Assert.Contains(results, x => x.Code == "I10");
    }

    [Fact]
    public async Task SearchDrapMedicinesAsync_ShouldReturnDefaultDrapCatalogMatches()
    {
        await using var dbContext = CreateDbContext();
        var service = new ConsultationService(dbContext);

        var results = await service.SearchDrapMedicinesAsync("amox", CancellationToken.None);

        Assert.Contains(results, x => x.BrandName == "Amoxil" && x.GenericName == "Amoxicillin");
        Assert.All(results, x => Assert.False(string.IsNullOrWhiteSpace(x.DrapRegistrationNumber)));
    }

    [Fact]
    public async Task CheckDrugAllergiesAsync_ShouldWarnWhenMedicineMatchesPatientAllergy()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext, "[\"Penicillin\"]");
        var service = new ConsultationService(dbContext);

        var result = await service.CheckDrugAllergiesAsync(
            appointment.PatientId,
            new DrugAllergyCheckRequest([
                new PrescriptionItemRequest("Amoxil", "Amoxicillin", "500mg", "Oral", "1 capsule", "BID", 5, 10, null, true)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var warning = Assert.Single(result.Value);
        Assert.Equal("Penicillin", warning.MatchedAllergy);
        Assert.Equal("High", warning.Severity);
        Assert.Contains("Amoxil", warning.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GeneratePrescriptionPdfAsync_ShouldCreatePdfWithVerificationSignatureAndQr()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var service = CreateDocumentEnabledService(dbContext);

        var result = await service.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Acute pharyngitis",
                "J02.9",
                "Hydration advised.",
                null,
                [new PrescriptionItemRequest("Panadol", "Paracetamol", "500mg", "Oral", "1 tablet", "TID", 3, 9, "After meals", true)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var prescription = result.Value.Prescription;
        Assert.NotNull(prescription);
        Assert.NotEmpty(prescription.VerificationCode);
        Assert.NotEmpty(prescription.DigitalSignature);

        var verification = await service.VerifyPrescriptionAsync(prescription.Id, prescription.VerificationCode, CancellationToken.None);
        Assert.True(verification.IsSuccess);
        Assert.True(verification.Value.IsValid);
        Assert.Equal(prescription.DigitalSignature, verification.Value.DigitalSignature);

        var pdf = await service.GeneratePrescriptionPdfAsync(prescription.Id, CancellationToken.None);

        Assert.True(pdf.IsSuccess);
        Assert.Equal("application/pdf", pdf.Value.ContentType);
        Assert.EndsWith(".pdf", pdf.Value.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.Value.Content, 0, 4), StringComparison.Ordinal);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static ConsultationService CreateDocumentEnabledService(HealthCareDbContext dbContext)
    {
        return new ConsultationService(
            dbContext,
            new QuestPdfPrescriptionDocumentService(),
            Options.Create(new PrescriptionDocumentOptions
            {
                VerificationBaseUrl = "https://verify.healthcarems.local/api/v1/consultations/prescriptions"
            }));
    }

    private static async Task<Appointment> SeedAppointmentAsync(HealthCareDbContext dbContext, string allergies = "[]")
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Ayesha",
            LastName = "Khan",
            Email = $"patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Hamza",
            LastName = "Raza",
            Email = $"doctor-{Guid.NewGuid():N}@example.com",
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
        patient.MedicalHistory = new MedicalHistory
        {
            Patient = patient,
            PatientId = patient.Id,
            Allergies = allergies
        };

        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = "PMDC-CONSULT",
            Specialization = "Family Medicine",
            Qualification = "MBBS",
            Biography = "Clinic doctor",
            City = "Lahore",
            ConsultationFee = 1800m,
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
            Type = AppointmentType.OnSite,
            Status = AppointmentStatus.InProgress,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Patient has fever and cough",
            ConsultationFee = doctor.ConsultationFee,
            QueueNumber = 1,
            CheckedInAt = scheduledAt.AddMinutes(-5)
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

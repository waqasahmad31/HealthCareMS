using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Application.Patients;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Consultations;
using HealthCareMS.Infrastructure.Doctors;
using HealthCareMS.Infrastructure.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Pharmacy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Integration;

public sealed class OnlineConsultationAndStockManagementTests
{
    [Fact]
    public async Task OnlineConsultationE2E_ShouldCompleteWithVitalsChatAndDoctorReview()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedOnlineAppointmentAsync(dbContext);
        var sessionService = new ConsultationSessionService(
            dbContext,
            Options.Create(new ConsultationSessionOptions
            {
                AppId = "test-agora-app",
                AppCertificate = "test-agora-secret",
                ClientBaseUrl = "http://localhost:5157",
                TokenExpiryMinutes = 60
            }));
        var chatService = new ConsultationChatService(
            dbContext,
            new NoopChatFileStorage(),
            Options.Create(new ChatFileStorageOptions()));
        var patientService = new PatientService(dbContext, new Pbkdf2PasswordHasher());
        var consultationService = new ConsultationService(dbContext, new QuestPdfPrescriptionDocumentService());
        var reviewService = new DoctorReviewService(dbContext);

        var started = await sessionService.StartAsync(new StartConsultationSessionRequest(appointment.Id), CancellationToken.None);
        var patientJoin = await sessionService.JoinAsync(started.Value.Id, new JoinConsultationSessionRequest("Patient"), CancellationToken.None);
        var doctorJoin = await sessionService.JoinAsync(started.Value.Id, new JoinConsultationSessionRequest("Doctor"), CancellationToken.None);
        var chat = await chatService.SendMessageAsync(
            started.Value.Id,
            new SendChatMessageRequest("Patient", "S18 Patient", "Connection is stable on mobile data."),
            appointment.Patient.UserId,
            CancellationToken.None);
        var vitals = await patientService.RecordVitalsAsync(
            appointment.PatientId,
            new RecordVitalsRequest(DateTimeOffset.UtcNow, 132, 84, 82, 154m, "Random", 73.4m, 37.1m, "3G E2E smoke"),
            CancellationToken.None);
        var complete = await consultationService.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Type 2 diabetes follow-up",
                "E11.9",
                "Reviewed vitals, chat, and video session.",
                null,
                [new PrescriptionItemRequest("Glucophage", "Metformin", "500mg", "Oral", "1 tablet", "BID", 30, 60, "After meals", true)]),
            CancellationToken.None);
        var review = await reviewService.SubmitReviewAsync(
            appointment.Id,
            new SubmitDoctorReviewRequest(5, "Clear guidance and smooth mobile consultation.", true),
            CancellationToken.None);
        var rating = await reviewService.GetDoctorRatingSummaryAsync(appointment.DoctorId, CancellationToken.None);

        Assert.True(started.IsSuccess);
        Assert.True(patientJoin.IsSuccess);
        Assert.True(doctorJoin.IsSuccess);
        Assert.Equal("InProgress", doctorJoin.Value.Session.Status);
        Assert.True(chat.IsSuccess);
        Assert.True(vitals.IsSuccess);
        Assert.True(complete.IsSuccess);
        Assert.True(review.IsSuccess);
        Assert.True(rating.IsSuccess);
        Assert.Equal(5m, rating.Value.AverageRating);
        Assert.Equal(1, rating.Value.RatingCount);
    }

    [Fact]
    public async Task PharmacyStockManagement_ShouldSelectFifoAdjustBatchAndFireAlerts()
    {
        await using var dbContext = CreateDbContext();
        var service = new PharmacyService(dbContext, new TestDistributedQueryCache());
        var medicine = await service.CreateMedicineAsync(
            new CreateMedicineRequest(
                null,
                "Ibuprofen",
                "FIFO Brufen",
                "Tablet",
                "400mg",
                $"DRAP-FIFO-{Guid.NewGuid():N}",
                "Abbott",
                35m,
                22m,
                false,
                50,
                null),
            CancellationToken.None);
        var firstBatch = await service.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(null, null, "FIFO-001", null, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(25)), 10, 22m),
            CancellationToken.None);
        var secondBatch = await service.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(null, null, "FIFO-002", null, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(55)), 20, 22m),
            CancellationToken.None);

        var trackedFirst = await dbContext.StockBatches.SingleAsync(x => x.Id == firstBatch.Value.Id);
        var trackedSecond = await dbContext.StockBatches.SingleAsync(x => x.Id == secondBatch.Value.Id);
        trackedFirst.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-8);
        trackedSecond.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-2);
        await dbContext.SaveChangesAsync();

        var selection = await service.GetFifoBatchSelectionAsync(medicine.Value.Id, 15, CancellationToken.None);
        var dispense = await service.DispenseFifoAsync(
            medicine.Value.Id,
            new FifoDispenseRequest(15, "S20 FIFO dispense verification"),
            CancellationToken.None);
        var adjustment = await service.AdjustStockBatchAsync(
            secondBatch.Value.Id,
            new AdjustStockBatchRequest(5, "Increase", "Stock recount correction"),
            CancellationToken.None);
        var alerts = await service.RunStockAlertScanAsync(CancellationToken.None);

        var batches = await dbContext.StockBatches.OrderBy(x => x.BatchNumber).ToListAsync();

        Assert.True(selection.IsSuccess);
        Assert.True(selection.Value.IsFulfillable);
        Assert.Equal("FIFO-001", selection.Value.Batches[0].BatchNumber);
        Assert.Equal(10, selection.Value.Batches[0].QuantitySelected);
        Assert.Equal("FIFO-002", selection.Value.Batches[1].BatchNumber);
        Assert.Equal(5, selection.Value.Batches[1].QuantitySelected);
        Assert.True(dispense.IsSuccess);
        Assert.Equal(15, dispense.Value.QuantityDispensed);
        Assert.True(adjustment.IsSuccess);
        Assert.Equal(20, adjustment.Value.NewQuantity);
        Assert.Equal(0, batches.Single(x => x.BatchNumber == "FIFO-001").QuantityOnHand);
        Assert.Equal(20, batches.Single(x => x.BatchNumber == "FIFO-002").QuantityOnHand);
        Assert.True(alerts.IsSuccess);
        Assert.Contains(alerts.Value, x => x.AlertType == "LowStock");
        Assert.Contains(alerts.Value, x => x.AlertType == "Expiry60Days");
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static async Task<Appointment> SeedOnlineAppointmentAsync(HealthCareDbContext dbContext)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "S18",
            LastName = "Patient",
            Email = $"s18-patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "S18",
            LastName = "Doctor",
            Email = $"s18-doctor-{Guid.NewGuid():N}@example.com",
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
            DateOfBirth = new DateOnly(1992, 5, 10),
            Gender = Gender.Female,
            Phone = "+923001234567",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = $"PMDC-S18-{Guid.NewGuid():N}",
            Specialization = "Internal Medicine",
            Qualification = "MBBS",
            Biography = "Online consultation reviewer",
            City = "Lahore",
            ConsultationFee = 2500m,
            IsVerified = true,
            IsActive = true
        };
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(45);
        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-{scheduledAt.UtcDateTime:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}",
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = scheduledAt,
            EndAt = scheduledAt.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.Online,
            Status = AppointmentStatus.Confirmed,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "S18 online consultation E2E verification",
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

    private sealed class NoopChatFileStorage : IChatFileStorage
    {
        public Task<StoredChatFile> SaveAsync(
            Guid sessionId,
            string fileName,
            string contentType,
            Stream content,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new StoredChatFile("noop", fileName, contentType, 0));
        }

        public Task<HealthCareMS.Shared.Common.Result<ChatAttachmentDownloadResponse>> OpenReadAsync(
            string storagePath,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(HealthCareMS.Shared.Common.Result<ChatAttachmentDownloadResponse>.Failure(
                new HealthCareMS.Shared.Common.Error("NOOP", "No file.")));
        }
    }
}

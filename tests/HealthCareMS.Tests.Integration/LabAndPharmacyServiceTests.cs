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

    [Fact]
    public async Task PharmacyService_ShouldDispensePrescriptionWithReceiptHistoryAndFifoStock()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var consultationService = new ConsultationService(dbContext, new QuestPdfPrescriptionDocumentService());
        var pharmacyService = new PharmacyService(dbContext);

        var complete = await consultationService.CompleteAsync(
            appointment.Id,
            new CompleteConsultationRequest(
                "Acute fever",
                "R50.9",
                "Dispense prescribed antipyretic.",
                null,
                [new PrescriptionItemRequest("Panadol", "Paracetamol", "500mg", "Oral", "1 tablet", "QID", 3, 12, "After meals", true)]),
            CancellationToken.None);
        var medicine = await pharmacyService.CreateMedicineAsync(
            new CreateMedicineRequest(
                null,
                "Paracetamol",
                "Panadol",
                "Tablet",
                "500mg",
                $"DRAP-DISP-{Guid.NewGuid():N}",
                "GSK Pakistan",
                25m,
                12m,
                false,
                10,
                null),
            CancellationToken.None);
        var firstBatch = await pharmacyService.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(null, null, "DSP-FIFO-001", null, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)), 5, 12m),
            CancellationToken.None);
        var secondBatch = await pharmacyService.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(null, null, "DSP-FIFO-002", null, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2)), 20, 12m),
            CancellationToken.None);

        var trackedFirst = await dbContext.StockBatches.SingleAsync(x => x.Id == firstBatch.Value.Id);
        var trackedSecond = await dbContext.StockBatches.SingleAsync(x => x.Id == secondBatch.Value.Id);
        trackedFirst.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-3);
        trackedSecond.ReceivedAt = DateTimeOffset.UtcNow.AddDays(-1);
        await dbContext.SaveChangesAsync();

        var prescription = complete.Value.Prescription!;
        var lookup = await pharmacyService.GetPrescriptionForDispensingAsync(
            prescription.Id,
            prescription.VerificationCode,
            CancellationToken.None);
        var dispense = await pharmacyService.DispensePrescriptionAsync(
            prescription.Id,
            new DispensePrescriptionRequest(
                prescription.VerificationCode,
                [new DispensePrescriptionItemRequest(prescription.Items.Single().Id, medicine.Value.Id, 12)],
                "S21 receipt verification"),
            CancellationToken.None);
        var history = await pharmacyService.GetDispensingHistoryAsync("Panadol", CancellationToken.None);
        var receipt = await pharmacyService.GenerateDispenseReceiptPdfAsync(dispense.Value.Id, CancellationToken.None);
        var batches = await dbContext.StockBatches.OrderBy(x => x.BatchNumber).ToListAsync();

        Assert.True(complete.IsSuccess);
        Assert.True(lookup.IsSuccess);
        Assert.True(lookup.Value.IsDispensable);
        Assert.True(dispense.IsSuccess);
        Assert.StartsWith("DSP-", dispense.Value.DispenseNumber, StringComparison.Ordinal);
        Assert.StartsWith("RCT-", dispense.Value.ReceiptNumber, StringComparison.Ordinal);
        Assert.Equal(300m, dispense.Value.TotalAmount);
        Assert.Equal(2, dispense.Value.Items.Single().Batches.Count);
        Assert.True(history.IsSuccess);
        Assert.Contains(history.Value, x => x.Id == dispense.Value.Id);
        Assert.True(receipt.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(receipt.Value.Content, 0, 4), StringComparison.Ordinal);
        Assert.Equal(0, batches.Single(x => x.BatchNumber == "DSP-FIFO-001").QuantityOnHand);
        Assert.Equal(13, batches.Single(x => x.BatchNumber == "DSP-FIFO-002").QuantityOnHand);
    }

    [Fact]
    public async Task PharmacyService_ShouldPlaceConfirmAssignAndDeliverOnlineOrder()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var pharmacyService = new PharmacyService(dbContext);
        var deliveryRole = new Role { Name = $"Delivery-{Guid.NewGuid():N}", IsSystemRole = true };
        var deliveryAgent = new ApplicationUser
        {
            Role = deliveryRole,
            RoleId = deliveryRole.Id,
            FirstName = "Delivery",
            LastName = "Agent",
            Email = $"delivery-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        dbContext.Roles.Add(deliveryRole);
        dbContext.Users.Add(deliveryAgent);
        await dbContext.SaveChangesAsync();

        var medicine = await pharmacyService.CreateMedicineAsync(
            new CreateMedicineRequest(
                null,
                "Cetirizine",
                "Zyrtec Online",
                "Tablet",
                "10mg",
                $"DRAP-ORD-{Guid.NewGuid():N}",
                "Martin Dow",
                30m,
                12m,
                false,
                5,
                null),
            CancellationToken.None);
        var batch = await pharmacyService.CreateStockBatchAsync(
            medicine.Value.Id,
            new CreateStockBatchRequest(null, null, "ORD-FIFO-001", null, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)), 30, 12m),
            CancellationToken.None);

        var order = await pharmacyService.CreateOrderAsync(
            new CreatePharmacyOrderRequest(
                null,
                appointment.PatientId,
                null,
                "House 1, Lahore",
                null,
                null,
                "Please deliver after 6 PM.",
                "prescription.jpg",
                "image/jpeg",
                [1, 2, 3, 4],
                [new CreatePharmacyOrderItemRequest(medicine.Value.Id, 8)]),
            CancellationToken.None);
        var confirmed = await pharmacyService.ConfirmOrderAsync(
            order.Value.Id,
            new ConfirmPharmacyOrderRequest(deliveryAgent.Id, "Prescription reviewed."),
            CancellationToken.None);
        var assigned = await pharmacyService.GetOrdersAsync(null, null, deliveryAgent.Id, CancellationToken.None);
        var dispatched = await pharmacyService.UpdateOrderStatusAsync(
            order.Value.Id,
            new UpdatePharmacyOrderStatusRequest("Dispatched", "Out for delivery."),
            CancellationToken.None);
        var delivered = await pharmacyService.UpdateOrderStatusAsync(
            order.Value.Id,
            new UpdatePharmacyOrderStatusRequest("Delivered", "Handed over to patient."),
            CancellationToken.None);
        var trackedBatch = await dbContext.StockBatches.SingleAsync(x => x.Id == batch.Value.Id);

        Assert.True(order.IsSuccess);
        Assert.StartsWith("PHO-", order.Value.OrderNumber, StringComparison.Ordinal);
        Assert.Equal("Placed", order.Value.Status);
        Assert.True(order.Value.HasPrescriptionUpload);
        Assert.Equal(490m, order.Value.TotalAmount);
        Assert.True(confirmed.IsSuccess);
        Assert.Equal("AssignedForDelivery", confirmed.Value.Status);
        Assert.Equal(deliveryAgent.Id, confirmed.Value.DeliveryAgentUserId);
        Assert.True(assigned.IsSuccess);
        Assert.Contains(assigned.Value, x => x.Id == order.Value.Id);
        Assert.True(dispatched.IsSuccess);
        Assert.Equal("Dispatched", dispatched.Value.Status);
        Assert.True(delivered.IsSuccess);
        Assert.Equal("Delivered", delivered.Value.Status);
        Assert.Equal(22, trackedBatch.QuantityOnHand);
    }

    [Fact]
    public async Task LabService_ShouldSeedLargeCatalogueImportCsvAndCreatePanel()
    {
        await using var dbContext = CreateDbContext();
        var service = new LabService(dbContext);

        var tests = await service.SearchTestsAsync(null, CancellationToken.None);
        var csv = """
            TestCode,TestName,Category,SampleType,TurnaroundHours,Price,IsHomeCollectionAvailable,FastingHours,PreparationInstructions,HomeCollectionExtra
            VIT-D3,Vitamin D3 Level,Endocrinology,Blood,24,2400,true,,No special preparation.,300
            """;
        var imported = await service.ImportTestsCsvAsync(new ImportLabTestsCsvRequest(null, csv), CancellationToken.None);
        var panel = await service.CreatePanelAsync(
            new CreateLabPanelRequest(
                null,
                $"PNL-{Guid.NewGuid().ToString("N")[..8]}",
                "Metabolic Wellness Panel",
                "Wellness",
                "Core metabolic screening bundle.",
                null,
                tests.Take(3).Select(x => x.Id).ToList()),
            CancellationToken.None);
        var panels = await service.GetPanelsAsync("wellness", CancellationToken.None);

        Assert.True(tests.Count >= 100);
        Assert.True(imported.IsSuccess);
        Assert.Equal(1, imported.Value.ImportedCount);
        Assert.True(panel.IsSuccess);
        Assert.Equal(3, panel.Value.Tests.Count);
        Assert.True(panel.Value.Price > 0);
        Assert.True(panels.IsSuccess);
        Assert.Contains(panels.Value, x => x.Id == panel.Value.Id);
    }

    [Fact]
    public async Task LabService_ShouldCreateOnSiteBookingCheckInAndGenerateBarcodeLabel()
    {
        await using var dbContext = CreateDbContext();
        var appointment = await SeedAppointmentAsync(dbContext);
        var service = new LabService(dbContext);
        var tests = await service.SearchTestsAsync("cbc", CancellationToken.None);

        var booking = await service.CreateBookingAsync(
            new CreateLabBookingRequest(
                null,
                appointment.PatientId,
                [tests.First().Id],
                "OnSite",
                DateTimeOffset.UtcNow.AddHours(2),
                null,
                "Patient will fast before sample."),
            CancellationToken.None);
        var checkIn = await service.CheckInBookingAsync(
            booking.Value.Id,
            new CheckInLabBookingRequest(true, "Fasting verified at desk."),
            CancellationToken.None);
        var label = await service.GenerateBarcodeLabelPdfAsync(booking.Value.Id, CancellationToken.None);
        var queue = await service.GetBookingsAsync("CheckedIn", "OnSite", null, CancellationToken.None);

        Assert.True(booking.IsSuccess);
        Assert.StartsWith("LAB-", booking.Value.BookingNumber, StringComparison.Ordinal);
        Assert.StartsWith("LQ-", booking.Value.TokenNumber, StringComparison.Ordinal);
        Assert.StartsWith("SMP-", booking.Value.SampleBarcode, StringComparison.Ordinal);
        Assert.Equal("Ordered", booking.Value.Status);
        Assert.True(checkIn.IsSuccess);
        Assert.Equal("CheckedIn", checkIn.Value.Status);
        Assert.True(checkIn.Value.FastingVerified);
        Assert.NotNull(checkIn.Value.CheckedInAt);
        Assert.True(label.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(label.Value.Content, 0, 4), StringComparison.Ordinal);
        Assert.True(queue.IsSuccess);
        Assert.Contains(queue.Value, x => x.Id == booking.Value.Id);
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

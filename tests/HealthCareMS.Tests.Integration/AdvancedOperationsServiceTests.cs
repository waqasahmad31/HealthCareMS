using System.Security.Cryptography;
using System.Text;
using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Auth;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Application.Identity;
using HealthCareMS.Application.Insights;
using HealthCareMS.Application.Labs;
using HealthCareMS.Application.Payments;
using HealthCareMS.Application.Security;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Doctors;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Domain.Notifications;
using HealthCareMS.Domain.Payments;
using HealthCareMS.Domain.Patients;
using HealthCareMS.Domain.Pharmacy;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Doctors;
using HealthCareMS.Infrastructure.Insights;
using HealthCareMS.Infrastructure.Labs;
using HealthCareMS.Infrastructure.Payments;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Integration;

public sealed class AdvancedOperationsServiceTests
{
    [Fact]
    public async Task PaymentService_ShouldCreateCheckoutCaptureRefundAndInvoiceDocument()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientDoctorGraphAsync(dbContext);

        var medicine = new Medicine
        {
            GenericName = "Paracetamol",
            BrandName = "Panadol Premium",
            DosageForm = "Tablet",
            Strength = "500mg",
            DrapRegistrationNumber = $"DRAP-PAY-{Guid.NewGuid():N}",
            Manufacturer = "GSK Pakistan",
            UnitPrice = 150m,
            UnitCostPrice = 80m,
            Barcode = $"PAY-{Guid.NewGuid():N}",
            ReorderLevel = 10,
            IsActive = true
        };

        var order = new PharmacyOrder
        {
            OrderNumber = $"PHO-{DateTimeOffset.UtcNow:yyyyMMdd}-900001",
            Patient = seed.Patient,
            PatientId = seed.Patient.Id,
            Status = PharmacyOrderStatus.Placed,
            OrderedAt = DateTimeOffset.UtcNow.AddMinutes(-25),
            DeliveryAddress = "House 22, DHA Phase 6, Lahore",
            PatientNotes = "Contactless delivery requested.",
            SubTotal = 600m,
            DeliveryFee = 120m,
            TotalAmount = 720m,
            Items =
            [
                new PharmacyOrderItem
                {
                    Medicine = medicine,
                    MedicineId = medicine.Id,
                    MedicineName = medicine.BrandName,
                    Quantity = 4,
                    UnitPrice = 150m,
                    LineTotal = 600m
                }
            ]
        };

        dbContext.Medicines.Add(medicine);
        dbContext.PharmacyOrders.Add(order);
        await dbContext.SaveChangesAsync();

        var service = new PaymentService(
            dbContext,
            Options.Create(new PaymentGatewayOptions()),
            new FakeCurrentUser(seed.PatientUser.Id, null, false, []));

        var checkout = await service.CreatePharmacyOrderCheckoutAsync(
            order.Id,
            new CreateOrderPaymentRequest(
                "JazzCash",
                "https://app.healthcarems.local/payments/return",
                "https://app.healthcarems.local/payments/cancel",
                "127.0.0.1"),
            CancellationToken.None);
        var captured = await service.SimulateGatewayCallbackAsync(
            checkout.Value.Transaction.Id,
            new SimulatePaymentCallbackRequest("Succeeded", "JAZZ-SETTLED-0001"),
            CancellationToken.None);
        var refunded = await service.RefundTransactionAsync(
            checkout.Value.Transaction.Id,
            new RefundPaymentRequest(200m, "Customer cancelled one medicine line."),
            CancellationToken.None);
        var payment = await service.GetOrderPaymentAsync(order.Id, CancellationToken.None);
        var invoice = await service.GetInvoiceAsync(checkout.Value.Transaction.Invoice!.Id, CancellationToken.None);
        var pdf = await service.GenerateInvoicePdfAsync(invoice.Value.Id, CancellationToken.None);

        Assert.True(checkout.IsSuccess);
        Assert.Equal("AwaitingCustomerAction", checkout.Value.Transaction.Status);
        Assert.Contains("session=", checkout.Value.HostedPaymentUrl, StringComparison.Ordinal);
        Assert.True(captured.IsSuccess);
        Assert.Equal("Succeeded", captured.Value.Status);
        Assert.Equal("Paid", captured.Value.Invoice!.Status);
        Assert.True(refunded.IsSuccess);
        Assert.Equal("Completed", refunded.Value.Status);
        Assert.Equal(200m, refunded.Value.Amount);
        Assert.True(payment.IsSuccess);
        Assert.Equal("PartiallyRefunded", payment.Value.Status);
        Assert.Single(payment.Value.Refunds);
        Assert.True(invoice.IsSuccess);
        Assert.Equal("PartiallyRefunded", invoice.Value.Status);
        Assert.True(pdf.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.Value.Content, 0, 4), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LabService_ShouldExecuteHomeCollectionCriticalReleaseAndAddendumWorkflow()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientDoctorGraphAsync(dbContext, AppointmentStatus.InProgress);

        var agentRole = new Role { Name = $"CollectionAgent-{Guid.NewGuid():N}", IsSystemRole = true };
        var collectionAgent = new ApplicationUser
        {
            Role = agentRole,
            RoleId = agentRole.Id,
            FirstName = "Rafay",
            LastName = "Collector",
            Email = $"collector-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };

        dbContext.Roles.Add(agentRole);
        dbContext.Users.Add(collectionAgent);
        await dbContext.SaveChangesAsync();

        var service = new LabService(dbContext, new TestDistributedQueryCache());
        var catalogue = await service.SearchTestsAsync("cbc", CancellationToken.None);
        var scheduledAt = DateTimeOffset.UtcNow.AddHours(3);

        var booking = await service.CreateConsultationLabOrderAsync(
            seed.Appointment.Id,
            new CreateConsultationLabOrderRequest(
                [catalogue.First().Id],
                "Home",
                scheduledAt,
                "Block H, Johar Town, Lahore",
                "Patient requested urgent home collection.",
                scheduledAt.AddHours(2)),
            CancellationToken.None);
        var assigned = await service.AssignCollectionAgentAsync(
            booking.Value.Id,
            new AssignLabCollectionAgentRequest(
                collectionAgent.Id,
                scheduledAt,
                scheduledAt.AddHours(2),
                "Assigned to Route A."),
            CancellationToken.None);
        var assignedCollections = await service.GetAssignedCollectionsAsync(collectionAgent.Id, null, CancellationToken.None);
        var started = await service.StartCollectionAsync(
            booking.Value.Id,
            new StartLabCollectionRequest("Agent en route."),
            CancellationToken.None);
        var collected = await service.MarkSampleCollectedAsync(
            booking.Value.Id,
            new MarkLabSampleCollectedRequest(true, "Sample collected at doorstep."),
            CancellationToken.None);
        var entered = await service.EnterResultsAsync(
            booking.Value.Id,
            new EnterLabResultsRequest(
            [
                new UpsertLabTestResultRequest(
                    booking.Value.Items.Single().Id,
                    "Hyperglycemia detected and escalated.",
                    [
                        new LabResultParameterRequest(
                            "Glucose",
                            "240",
                            "mg/dL",
                            70m,
                            110m,
                            null,
                            null,
                            200m,
                            "Critical high result")
                    ])
            ]),
            CancellationToken.None);
        var resultId = entered.Value.Single().Id;
        var acknowledged = await service.AcknowledgeCriticalAlertAsync(
            resultId,
            new AcknowledgeLabCriticalAlertRequest("Doctor notified immediately."),
            CancellationToken.None);
        var queue = await service.GetValidationQueueAsync("critical", CancellationToken.None);
        var techValidated = await service.ValidateResultsAsync(
            booking.Value.Id,
            new ValidateLabResultsRequest("Tech", "Analyzer values cross-checked."),
            CancellationToken.None);
        var managerValidated = await service.ValidateResultsAsync(
            booking.Value.Id,
            new ValidateLabResultsRequest("Manager", "Manager approved release."),
            CancellationToken.None);
        var released = await service.ReleaseResultsAsync(
            booking.Value.Id,
            new ReleaseLabResultsRequest("Initial release issued."),
            CancellationToken.None);
        var addendum = await service.AddAddendumAsync(
            resultId,
            new AddLabResultAddendumRequest("Repeat sample confirmed the elevated value."),
            CancellationToken.None);
        var reTechValidated = await service.ValidateResultsAsync(
            booking.Value.Id,
            new ValidateLabResultsRequest("Tech", "Correction validated by technician."),
            CancellationToken.None);
        var reManagerValidated = await service.ValidateResultsAsync(
            booking.Value.Id,
            new ValidateLabResultsRequest("Manager", "Correction approved by manager."),
            CancellationToken.None);
        var reReleased = await service.ReleaseResultsAsync(
            booking.Value.Id,
            new ReleaseLabResultsRequest("Corrected report re-released."),
            CancellationToken.None);
        var report = await service.GenerateReportPdfAsync(booking.Value.Id, CancellationToken.None);
        var patientResults = await service.GetPatientResultsAsync(seed.Patient.Id, CancellationToken.None);
        var persistedBooking = await dbContext.LabSampleBookings.SingleAsync(x => x.Id == booking.Value.Id);
        var verification = await service.VerifyReportAsync(
            booking.Value.Id,
            persistedBooking.ReportVerificationCode!,
            CancellationToken.None);
        var notifications = await dbContext.Notifications
            .Where(x => x.ReferenceType == "LabBooking" && x.ReferenceId == booking.Value.Id)
            .ToListAsync();

        Assert.True(booking.IsSuccess);
        Assert.Equal("Home", booking.Value.CollectionType);
        Assert.True(assigned.IsSuccess);
        Assert.Equal("AgentAssigned", assigned.Value.Status);
        Assert.True(assignedCollections.IsSuccess);
        Assert.Contains(assignedCollections.Value, x => x.Id == booking.Value.Id);
        Assert.True(started.IsSuccess);
        Assert.Equal("InTransit", started.Value.Status);
        Assert.True(collected.IsSuccess);
        Assert.Equal("SampleCollected", collected.Value.Status);
        Assert.True(entered.IsSuccess);
        Assert.True(entered.Value.Single().HasCriticalValue);
        Assert.True(acknowledged.IsSuccess);
        Assert.NotNull(acknowledged.Value.CriticalAlertAcknowledgedAt);
        Assert.True(queue.IsSuccess);
        Assert.Contains(queue.Value, x => x.BookingId == booking.Value.Id);
        Assert.True(techValidated.IsSuccess);
        Assert.All(techValidated.Value, x => Assert.Equal("TechValidated", x.Status));
        Assert.True(managerValidated.IsSuccess);
        Assert.All(managerValidated.Value, x => Assert.Equal("ManagerValidated", x.Status));
        Assert.True(released.IsSuccess);
        Assert.All(released.Value, x => Assert.Equal("Released", x.Status));
        Assert.True(addendum.IsSuccess);
        Assert.Equal("Corrected", addendum.Value.Status);
        Assert.True(reTechValidated.IsSuccess);
        Assert.True(reManagerValidated.IsSuccess);
        Assert.True(reReleased.IsSuccess);
        Assert.True(report.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(report.Value.Content, 0, 4), StringComparison.Ordinal);
        Assert.True(patientResults.IsSuccess);
        Assert.Contains(patientResults.Value, x => x.BookingId == booking.Value.Id);
        Assert.True(verification.IsSuccess);
        Assert.True(verification.Value.IsVerified);
        Assert.True(notifications.Count >= 2);
    }

    [Fact]
    public async Task SecurityCenterAndAuth_ShouldManageTwoFactorSessionsAndLoginHistory()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var role = new Role { Name = "SuperAdmin", IsSystemRole = true };
        var permission = NewPermission(PermissionKeys.System.SuperAdminAll);
        role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, PermissionId = permission.Id });
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);

        var user = new ApplicationUser
        {
            Role = role,
            RoleId = role.Id,
            FirstName = "Platform",
            LastName = "Admin",
            Email = "platform.admin@example.com",
            PasswordHash = hasher.Hash("StrongPass123"),
            IsActive = true,
            IsEmailVerified = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, hasher, CreateJwtService());
        var securityService = new SecurityCenterService(dbContext);

        var initialLogin = await authService.LoginAsync(
            new LoginRequest(user.Email, "StrongPass123"),
            CancellationToken.None);
        var pre2FaOverview = await securityService.GetOverviewAsync(user.Id, CancellationToken.None);
        var setup = await securityService.BeginTwoFactorSetupAsync(user.Id, CancellationToken.None);
        var enable = await securityService.EnableTwoFactorAsync(
            user.Id,
            new EnableTwoFactorRequest(GenerateTotpCode(setup.Value.SecretKey)),
            CancellationToken.None);
        var invalidLogin = await authService.LoginAsync(
            new LoginRequest(user.Email, "StrongPass123", "000000"),
            CancellationToken.None);
        var validLogin = await authService.LoginAsync(
            new LoginRequest(user.Email, "StrongPass123", GenerateTotpCode(setup.Value.SecretKey)),
            CancellationToken.None);
        var sessions = await securityService.GetActiveSessionsAsync(user.Id, CancellationToken.None);
        var revoke = await securityService.RevokeSessionAsync(
            user.Id,
            sessions.Value.OrderBy(x => x.IssuedAt).First().Id,
            CancellationToken.None);
        var history = await securityService.GetLoginHistoryAsync(user.Id, CancellationToken.None);
        var postRevokeOverview = await securityService.GetOverviewAsync(user.Id, CancellationToken.None);
        var disabled = await securityService.DisableTwoFactorAsync(user.Id, CancellationToken.None);

        Assert.True(initialLogin.IsSuccess);
        Assert.True(pre2FaOverview.IsSuccess);
        Assert.Equal(1, pre2FaOverview.Value.ActiveSessionCount);
        Assert.True(setup.IsSuccess);
        Assert.Contains("otpauth://totp/", setup.Value.OtpAuthUri, StringComparison.Ordinal);
        Assert.True(enable.IsSuccess);
        Assert.True(enable.Value.TwoFactorEnabled);
        Assert.True(invalidLogin.IsFailure);
        Assert.Equal("AUTH_2FA_INVALID", invalidLogin.Error.Code);
        Assert.True(validLogin.IsSuccess);
        Assert.True(sessions.IsSuccess);
        Assert.True(sessions.Value.Count >= 2);
        Assert.True(revoke.IsSuccess);
        Assert.NotNull(revoke.Value.RevokedAt);
        Assert.True(history.IsSuccess);
        Assert.Contains(history.Value, x => !x.IsSuccessful && x.FailureReason == "Two-factor validation failed");
        Assert.True(postRevokeOverview.IsSuccess);
        Assert.Equal(1, postRevokeOverview.Value.FailedLoginCount);
        Assert.Equal(1, postRevokeOverview.Value.ActiveSessionCount);
        Assert.True(disabled.IsSuccess);
        Assert.False(disabled.Value.TwoFactorEnabled);
    }

    [Fact]
    public async Task InsightAndRecommendationServices_ShouldReturnTimelineAnalyticsAndBestMatch()
    {
        await using var dbContext = CreateDbContext();
        var seed = await SeedPatientDoctorGraphAsync(dbContext, AppointmentStatus.Completed);

        seed.Patient.BloodGroup = "B+";
        var patientHistory = new MedicalHistory
        {
            Patient = seed.Patient,
            PatientId = seed.Patient.Id,
            Allergies = """["Dust"]""",
            ChronicDiseases = """["Diabetes"]""",
            FamilyHistory = """["Hypertension"]""",
            CurrentMedications = """["Metformin"]"""
        };
        seed.Patient.MedicalHistory = patientHistory;
        seed.Appointment.Diagnosis = "Diabetes follow-up";
        seed.Appointment.ClinicalNotes = "Reviewed sugars and reinforced diet plan.";
        seed.Appointment.Icd10Code = "E11.9";
        seed.Appointment.Icd10Title = "Type 2 diabetes mellitus without complications";
        seed.Appointment.PaymentStatus = Domain.Appointments.PaymentStatus.Paid;

        var recommendationDoctorRole = seed.DoctorUser.Role;
        var alternateDoctorUser = new ApplicationUser
        {
            Role = recommendationDoctorRole,
            RoleId = recommendationDoctorRole.Id,
            FirstName = "Zain",
            LastName = "Derm",
            Email = $"doctor-alt-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var alternateDoctor = new Doctor
        {
            User = alternateDoctorUser,
            UserId = alternateDoctorUser.Id,
            PmdcRegistrationNumber = $"PMDC-ALT-{Guid.NewGuid():N}",
            Specialization = "Dermatology",
            Qualification = "MBBS",
            Biography = "Skin specialist",
            City = "Lahore",
            ConsultationFee = 2400m,
            AverageRating = 4.9m,
            RatingCount = 20,
            IsVerified = true,
            IsActive = true
        };
        var alternateDoctorSchedule = new DoctorSchedule
        {
            Doctor = alternateDoctor,
            DoctorId = alternateDoctor.Id,
            DayOfWeek = seed.TargetDate.DayOfWeek,
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(13, 0),
            SlotDurationMinutes = 30,
            IsOnlineAvailable = true,
            IsOnSiteAvailable = true
        };

        seed.Doctor.Specialization = "Endocrinology";
        seed.Doctor.City = "Lahore";
        seed.Doctor.ConsultationFee = 2500m;
        seed.Doctor.AverageRating = 4.6m;
        seed.Doctor.RatingCount = 8;
        seed.Doctor.IsVerified = true;
        var bestDoctorSchedule = new DoctorSchedule
        {
            Doctor = seed.Doctor,
            DoctorId = seed.Doctor.Id,
            DayOfWeek = seed.TargetDate.DayOfWeek,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            SlotDurationMinutes = 30,
            IsOnlineAvailable = true,
            IsOnSiteAvailable = true
        };

        var vital = new PatientVital
        {
            Patient = seed.Patient,
            PatientId = seed.Patient.Id,
            RecordedAt = seed.Appointment.ScheduledAt.AddHours(-2),
            SystolicBloodPressure = 126,
            DiastolicBloodPressure = 82,
            HeartRate = 74,
            BloodSugarMgDl = 162m,
            BloodSugarContext = "Post-meal",
            WeightKg = 71.2m,
            TemperatureCelsius = 37m,
            Notes = "Captured at home."
        };

        var medicine = new Medicine
        {
            GenericName = "Metformin",
            BrandName = "Glucophage XR",
            DosageForm = "Tablet",
            Strength = "500mg",
            DrapRegistrationNumber = $"DRAP-INS-{Guid.NewGuid():N}",
            Manufacturer = "Merck",
            UnitPrice = 120m,
            UnitCostPrice = 75m,
            Barcode = $"INS-{Guid.NewGuid():N}",
            ReorderLevel = 15,
            IsActive = true
        };
        var pharmacyOrder = new PharmacyOrder
        {
            OrderNumber = $"PHO-{DateTimeOffset.UtcNow:yyyyMMdd}-910001",
            Patient = seed.Patient,
            PatientId = seed.Patient.Id,
            Status = PharmacyOrderStatus.Delivered,
            OrderedAt = seed.Appointment.ScheduledAt.AddHours(1),
            ConfirmedAt = seed.Appointment.ScheduledAt.AddHours(2),
            DeliveredAt = seed.Appointment.ScheduledAt.AddHours(6),
            DeliveryAddress = "Street 12, Lahore",
            SubTotal = 360m,
            DeliveryFee = 90m,
            TotalAmount = 450m,
            Items =
            [
                new PharmacyOrderItem
                {
                    Medicine = medicine,
                    MedicineId = medicine.Id,
                    MedicineName = medicine.BrandName,
                    Quantity = 3,
                    UnitPrice = 120m,
                    LineTotal = 360m
                }
            ]
        };
        var payment = new PaymentTransaction
        {
            PharmacyOrder = pharmacyOrder,
            PharmacyOrderId = pharmacyOrder.Id,
            ReferenceType = nameof(PharmacyOrder),
            ReferenceId = pharmacyOrder.Id,
            PaymentNumber = $"PAY-{DateTimeOffset.UtcNow:yyyyMMdd}-910001",
            Gateway = PaymentGateway.Stripe,
            Status = PaymentTransactionStatus.Succeeded,
            Amount = pharmacyOrder.TotalAmount,
            Currency = "PKR",
            SessionToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            CheckoutUrl = "https://sandbox.stripe.healthcarems.local/checkout",
            ExpiresAt = seed.Appointment.ScheduledAt.AddHours(2),
            PaidAt = seed.Appointment.ScheduledAt.AddHours(1.5)
        };

        var labTest = new LabTest
        {
            TestCode = "HBA1C",
            TestName = "HbA1c",
            Category = "Chemistry",
            SampleType = "Blood",
            TurnaroundHours = 12,
            Price = 1800m,
            IsHomeCollectionAvailable = true,
            HomeCollectionExtra = 300m
        };
        var labBooking = new LabSampleBooking
        {
            BookingNumber = $"LAB-{DateTimeOffset.UtcNow:yyyyMMdd}-910001",
            Patient = seed.Patient,
            PatientId = seed.Patient.Id,
            Appointment = seed.Appointment,
            AppointmentId = seed.Appointment.Id,
            CollectionType = LabCollectionType.OnSite,
            Status = LabBookingStatus.ResultsReleased,
            CreatedAt = seed.Appointment.ScheduledAt.AddHours(1),
            SampleCollectedAt = seed.Appointment.ScheduledAt.AddHours(2),
            ResultsReleasedAt = seed.Appointment.ScheduledAt.AddHours(16),
            ReportVerificationCode = "VERIFY-HBA1C-001",
            Notes = "Released to patient portal.",
            SubTotal = 1800m,
            HomeCollectionFee = 0m,
            TotalAmount = 1800m
        };
        var labBookingItem = new LabBookingItem
        {
            Booking = labBooking,
            BookingId = labBooking.Id,
            LabTest = labTest,
            LabTestId = labTest.Id,
            Price = 1800m
        };
        var labResult = new LabTestResult
        {
            LabSampleBooking = labBooking,
            LabSampleBookingId = labBooking.Id,
            LabBookingItem = labBookingItem,
            LabBookingItemId = labBookingItem.Id,
            LabTest = labTest,
            LabTestId = labTest.Id,
            ResultNumber = $"LRES-{DateTimeOffset.UtcNow:yyyyMMdd}-910001",
            Status = LabResultStatus.Released,
            ParametersJson = "[]",
            Summary = "HbA1c above goal.",
            IsAbnormal = true,
            ReleasedAt = seed.Appointment.ScheduledAt.AddHours(16)
        };

        dbContext.MedicalHistories.Add(patientHistory);
        dbContext.Users.Add(alternateDoctorUser);
        dbContext.Doctors.Add(alternateDoctor);
        dbContext.DoctorSchedules.Add(bestDoctorSchedule);
        dbContext.DoctorSchedules.Add(alternateDoctorSchedule);
        dbContext.PatientVitals.Add(vital);
        dbContext.Medicines.Add(medicine);
        dbContext.PharmacyOrders.Add(pharmacyOrder);
        dbContext.PaymentTransactions.Add(payment);
        dbContext.LabTests.Add(labTest);
        dbContext.LabSampleBookings.Add(labBooking);
        dbContext.LabBookingItems.Add(labBookingItem);
        dbContext.LabTestResults.Add(labResult);
        await dbContext.SaveChangesAsync();

        var insightService = new InsightService(dbContext);
        var doctorService = new DoctorService(dbContext, new Pbkdf2PasswordHasher(), new TestDistributedQueryCache());

        var timeline = await insightService.GetPatientTimelineAsync(seed.Patient.Id, null, CancellationToken.None);
        var labOnlyTimeline = await insightService.GetPatientTimelineAsync(seed.Patient.Id, "lab", CancellationToken.None);
        var summaryPdf = await insightService.GenerateHealthSummaryPdfAsync(seed.Patient.Id, CancellationToken.None);
        var emergencyPdf = await insightService.GenerateEmergencyCardPdfAsync(seed.Patient.Id, CancellationToken.None);
        var analytics = await insightService.GetAnalyticsAsync(
            null,
            seed.TargetDate.AddDays(-2),
            seed.TargetDate.AddDays(2),
            CancellationToken.None);
        var recommendations = await doctorService.GetRecommendationsAsync(
            new DoctorRecommendationRequest(
                seed.Patient.Id,
                "Endocrinology",
                "Lahore",
                3000m,
                seed.TargetDate,
                "Online"),
            CancellationToken.None);

        Assert.True(timeline.IsSuccess);
        Assert.Contains(timeline.Value.Entries, x => x.Type == "appointment");
        Assert.Contains(timeline.Value.Entries, x => x.Type == "vitals");
        Assert.Contains(timeline.Value.Entries, x => x.Type == "lab");
        Assert.Contains(timeline.Value.Entries, x => x.Type == "pharmacy");
        Assert.True(labOnlyTimeline.IsSuccess);
        Assert.All(labOnlyTimeline.Value.Entries, x => Assert.Equal("lab", x.Type));
        Assert.True(summaryPdf.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(summaryPdf.Value.Content, 0, 4), StringComparison.Ordinal);
        Assert.True(emergencyPdf.IsSuccess);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(emergencyPdf.Value.Content, 0, 4), StringComparison.Ordinal);
        Assert.True(analytics.IsSuccess);
        Assert.Equal(1, analytics.Value.TotalAppointments);
        Assert.Equal(1, analytics.Value.CompletedAppointments);
        Assert.Equal(2500m, analytics.Value.AppointmentRevenue);
        Assert.Equal(450m, analytics.Value.PharmacyRevenue);
        Assert.Equal(1800m, analytics.Value.LabRevenue);
        Assert.Equal(4750m, analytics.Value.TotalRevenue);
        Assert.Equal(100m, analytics.Value.PharmacyFulfillmentRatePercent);
        Assert.True(analytics.Value.AverageLabTurnaroundHours > 0);
        Assert.Equal(3, analytics.Value.ModuleRevenue.Count);
        Assert.NotEmpty(analytics.Value.DoctorUtilization);
        Assert.NotEmpty(recommendations);
        var bestMatch = recommendations.First();
        Assert.True(bestMatch.IsBestMatch);
        Assert.Equal(seed.Doctor.Id, bestMatch.Doctor.Id);
        Assert.Contains(bestMatch.MatchReasons, reason => reason.Contains("Specialization match", StringComparison.Ordinal));
        Assert.Contains(bestMatch.MatchReasons, reason => reason.Contains("History-aligned expertise", StringComparison.Ordinal));
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static JwtTokenService CreateJwtService()
    {
        return new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "HealthCareMS.Tests",
            Audience = "HealthCareMS.Tests.Client",
            SigningKey = "TEST_SIGNING_KEY_WITH_MORE_THAN_32_CHARS",
            AccessTokenMinutes = 60
        }));
    }

    private static async Task<SeedGraph> SeedPatientDoctorGraphAsync(
        HealthCareDbContext dbContext,
        AppointmentStatus appointmentStatus = AppointmentStatus.Completed)
    {
        var patientRole = new Role { Name = $"Patient-{Guid.NewGuid():N}", IsSystemRole = true };
        var doctorRole = new Role { Name = $"Doctor-{Guid.NewGuid():N}", IsSystemRole = true };
        var patientUser = new ApplicationUser
        {
            Role = patientRole,
            RoleId = patientRole.Id,
            FirstName = "Areesha",
            LastName = "Iqbal",
            Email = $"patient-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            PhoneNumber = "+923001112233",
            IsActive = true,
            IsEmailVerified = true
        };
        var doctorUser = new ApplicationUser
        {
            Role = doctorRole,
            RoleId = doctorRole.Id,
            FirstName = "Usman",
            LastName = "Hakeem",
            Email = $"doctor-{Guid.NewGuid():N}@example.com",
            PasswordHash = "HASH",
            PhoneNumber = "+923009998887",
            IsActive = true,
            IsEmailVerified = true
        };
        var patient = new Patient
        {
            User = patientUser,
            UserId = patientUser.Id,
            FirstName = patientUser.FirstName,
            LastName = patientUser.LastName,
            DateOfBirth = new DateOnly(1991, 2, 14),
            Gender = Gender.Female,
            Phone = patientUser.PhoneNumber,
            EmergencyContactName = "Bilal Iqbal",
            EmergencyContactPhone = "+923004445556",
            IsActive = true
        };
        var doctor = new Doctor
        {
            User = doctorUser,
            UserId = doctorUser.Id,
            PmdcRegistrationNumber = $"PMDC-{Guid.NewGuid():N}",
            Specialization = "General Medicine",
            Qualification = "MBBS, FCPS",
            Biography = "Consultant physician",
            City = "Lahore",
            ConsultationFee = 2500m,
            AverageRating = 4.5m,
            RatingCount = 6,
            IsVerified = true,
            IsActive = true
        };

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var appointmentTime = new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var appointment = new Appointment
        {
            AppointmentNumber = $"APT-{targetDate:yyyyMMdd}-700001",
            Patient = patient,
            PatientId = patient.Id,
            Doctor = doctor,
            DoctorId = doctor.Id,
            ScheduledAt = appointmentTime,
            EndAt = appointmentTime.AddMinutes(30),
            DurationMinutes = 30,
            Type = AppointmentType.Online,
            Status = appointmentStatus,
            Priority = AppointmentPriority.Normal,
            ReasonForVisit = "Specialist review",
            ConsultationFee = doctor.ConsultationFee
        };

        dbContext.Roles.AddRange(patientRole, doctorRole);
        dbContext.Users.AddRange(patientUser, doctorUser);
        dbContext.Patients.Add(patient);
        dbContext.Doctors.Add(doctor);
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync();

        return new SeedGraph(patientUser, doctorUser, patient, doctor, appointment, targetDate);
    }

    private static DateOnly Next(DayOfWeek dayOfWeek)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysToAdd = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysToAdd == 0)
        {
            daysToAdd = 7;
        }

        return today.AddDays(daysToAdd);
    }

    private static Permission NewPermission(string permissionKey)
    {
        var parts = permissionKey.Split('.');
        return new Permission
        {
            PermissionKey = permissionKey,
            Module = parts[0],
            Action = parts[^1],
            Description = permissionKey
        };
    }

    private static string GenerateTotpCode(string secret)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var secretBytes = Base32Decode(secret);
        var timestep = timestamp.ToUnixTimeSeconds() / 30;
        var counter = BitConverter.GetBytes(timestep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];

        return (binaryCode % 1_000_000).ToString("000000");
    }

    private static byte[] Base32Decode(string input)
    {
        const string base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var cleaned = input.Trim().TrimEnd('=').ToUpperInvariant();
        if (cleaned.Length == 0)
        {
            return [];
        }

        var output = new List<byte>(cleaned.Length * 5 / 8);
        var bitBuffer = 0;
        var bitsLeft = 0;

        foreach (var character in cleaned)
        {
            var value = base32Alphabet.IndexOf(character);
            if (value < 0)
            {
                continue;
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((bitBuffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return [.. output];
    }

    private sealed record SeedGraph(
        ApplicationUser PatientUser,
        ApplicationUser DoctorUser,
        Patient Patient,
        Doctor Doctor,
        Appointment Appointment,
        DateOnly TargetDate);

    private sealed class FakeCurrentUser(
        Guid? userId,
        Guid? tenantId,
        bool isSuperAdmin,
        IReadOnlyCollection<string> permissions) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId { get; } = tenantId;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsSuperAdmin { get; } = isSuperAdmin;

        public IReadOnlyCollection<string> Permissions { get; } = permissions;
    }
}

using System.Globalization;
using System.Text.Json;
using HealthCareMS.Application.Insights;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;
using HealthCareMS.Shared.Common;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Domain.Payments;

namespace HealthCareMS.Infrastructure.Insights;

public sealed class InsightService(
    HealthCareDbContext dbContext,
    IEmailSender? emailSender = null) : IInsightService
{
    public async Task<Result<PatientHealthTimelineResponse>> GetPatientTimelineAsync(
        Guid patientId,
        string? filter,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .Include(x => x.User)
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return Result<PatientHealthTimelineResponse>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var entries = await BuildTimelineEntriesAsync(patientId, cancellationToken);
        var normalizedFilter = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedFilter))
        {
            entries = entries.Where(x => string.Equals(x.Type, normalizedFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return Result<PatientHealthTimelineResponse>.Success(new PatientHealthTimelineResponse(
            patientId,
            patient.User.FullName,
            patient.BloodGroup,
            entries.OrderByDescending(x => x.OccurredAt).ToList()));
    }

    public async Task<Result<InsightDocumentResponse>> GenerateHealthSummaryPdfAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var timeline = await GetPatientTimelineAsync(patientId, filter: null, cancellationToken);
        if (timeline.IsFailure)
        {
            return Result<InsightDocumentResponse>.Failure(timeline.Error);
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .Include(x => x.MedicalHistory)
            .SingleAsync(x => x.Id == patientId, cancellationToken);

        var pdf = GenerateHealthSummaryPdf(patient, timeline.Value.Entries);
        return Result<InsightDocumentResponse>.Success(new InsightDocumentResponse(
            pdf,
            $"HealthSummary-{patient.User.FullName.Replace(' ', '-')}.pdf",
            "application/pdf"));
    }

    public async Task<Result<InsightDocumentResponse>> GenerateEmergencyCardPdfAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var patient = await dbContext.Patients
            .Include(x => x.User)
            .Include(x => x.MedicalHistory)
            .SingleOrDefaultAsync(x => x.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return Result<InsightDocumentResponse>.Failure(new Error("PATIENT_NOT_FOUND", "Patient was not found."));
        }

        var pdf = GenerateEmergencyCardPdf(patient);
        return Result<InsightDocumentResponse>.Success(new InsightDocumentResponse(
            pdf,
            $"EmergencyCard-{patient.User.FullName.Replace(' ', '-')}.pdf",
            "application/pdf"));
    }

    public async Task<Result<AnalyticsSnapshotResponse>> GetAnalyticsAsync(
        Guid? tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var window = ResolveWindow(from, to);
        var fromStart = new DateTimeOffset(window.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toEndExclusive = new DateTimeOffset(window.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var appointmentQuery = dbContext.Appointments
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .Where(x => x.ScheduledAt >= fromStart && x.ScheduledAt < toEndExclusive);
        if (tenantId.HasValue)
        {
            appointmentQuery = appointmentQuery.Where(x => x.Doctor.TenantId == tenantId);
        }

        var appointments = await appointmentQuery.ToListAsync(cancellationToken);

        var paymentsQuery = dbContext.PaymentTransactions
            .Where(x => x.PaidAt.HasValue && x.PaidAt.Value >= fromStart && x.PaidAt.Value < toEndExclusive);
        if (tenantId.HasValue)
        {
            paymentsQuery = paymentsQuery.Where(x => x.TenantId == tenantId);
        }

        var payments = await paymentsQuery
            .Include(x => x.Refunds)
            .ToListAsync(cancellationToken);

        var labQuery = dbContext.LabSampleBookings
            .Where(x => x.CreatedAt >= fromStart && x.CreatedAt < toEndExclusive);
        if (tenantId.HasValue)
        {
            labQuery = labQuery.Where(x => x.TenantId == tenantId);
        }

        var labBookings = await labQuery.ToListAsync(cancellationToken);

        var ordersQuery = dbContext.PharmacyOrders
            .Where(x => x.OrderedAt >= fromStart && x.OrderedAt < toEndExclusive);
        if (tenantId.HasValue)
        {
            ordersQuery = ordersQuery.Where(x => x.TenantId == tenantId);
        }

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        var appointmentRevenue = appointments
            .Where(x => x.Status == Domain.Appointments.AppointmentStatus.Completed)
            .Sum(x => x.ConsultationFee);
        var pharmacyRevenue = payments.Sum(x => x.Amount - x.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount));
        var labRevenue = labBookings
            .Where(x => x.Status != Domain.Labs.LabBookingStatus.Cancelled)
            .Sum(x => x.TotalAmount);
        var totalRevenue = appointmentRevenue + pharmacyRevenue + labRevenue;

        var doctorUtilization = appointments
            .GroupBy(x => new { x.DoctorId, x.Doctor.User.FullName })
            .Select(x => new DoctorUtilizationResponse(
                x.Key.DoctorId,
                x.Key.FullName,
                appointments.Count == 0 ? 0m : decimal.Round((decimal)x.Count() / appointments.Count * 100m, 2),
                x.Count(),
                x.Sum(item => item.ConsultationFee)))
            .OrderByDescending(x => x.UtilizationPercent)
            .ThenByDescending(x => x.Revenue)
            .Take(12)
            .ToList();

        var labTatHours = labBookings
            .Where(x => x.ResultsReleasedAt.HasValue && (x.SampleCollectedAt.HasValue || x.CheckedInAt.HasValue))
            .Select(x =>
            {
                var start = x.SampleCollectedAt ?? x.CheckedInAt ?? x.CreatedAt;
                return (decimal)(x.ResultsReleasedAt!.Value - start).TotalHours;
            })
            .ToList();

        var pharmacyFulfillmentRate = orders.Count == 0
            ? 0m
            : decimal.Round((decimal)orders.Count(x => x.Status == Domain.Pharmacy.PharmacyOrderStatus.Delivered) / orders.Count * 100m, 2);

        return Result<AnalyticsSnapshotResponse>.Success(new AnalyticsSnapshotResponse(
            window.From,
            window.To,
            appointments.Count,
            appointments.Count(x => x.Status == Domain.Appointments.AppointmentStatus.Completed),
            appointmentRevenue,
            pharmacyRevenue,
            labRevenue,
            totalRevenue,
            doctorUtilization.Count == 0 ? 0m : decimal.Round(doctorUtilization.Average(x => x.UtilizationPercent), 2),
            labTatHours.Count == 0 ? 0m : decimal.Round(labTatHours.Average(), 2),
            pharmacyFulfillmentRate,
            BuildAppointmentSeries(window.From, window.To, appointments),
            BuildRevenueSeries(window.From, window.To, appointments, payments, labBookings),
            [
                new ModuleRevenueResponse("Appointments", appointmentRevenue),
                new ModuleRevenueResponse("Pharmacy", pharmacyRevenue),
                new ModuleRevenueResponse("Laboratory", labRevenue)
            ],
            doctorUtilization));
    }

    public async Task SendScheduledAnalyticsEmailsAsync(CancellationToken cancellationToken)
    {
        if (emailSender is null)
        {
            return;
        }

        var admins = await dbContext.Users
            .Include(x => x.Role)
            .Where(x => x.IsActive && (x.Role.Name.Contains("Admin") || x.Role.Name.Contains("Super", StringComparison.OrdinalIgnoreCase)))
            .ToListAsync(cancellationToken);

        var snapshot = await GetAnalyticsAsync(tenantId: null, from: null, to: null, cancellationToken);
        if (snapshot.IsFailure)
        {
            return;
        }

        var body = BuildAnalyticsEmailBody(snapshot.Value);
        foreach (var admin in admins)
        {
            await emailSender.SendAsync(admin.Email, "HealthCareMS Analytics Digest", body, cancellationToken);
        }
    }

    private async Task<List<PatientHealthTimelineEntryResponse>> BuildTimelineEntriesAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Appointments
            .Include(x => x.Doctor)
            .ThenInclude(x => x.User)
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);
        var vitals = await dbContext.PatientVitals
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);
        var labs = await dbContext.LabSampleBookings
            .Include(x => x.Results)
            .ThenInclude(x => x.LabTest)
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);
        var orders = await dbContext.PharmacyOrders
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);
        var dispenses = await dbContext.PrescriptionDispenses
            .Where(x => x.PatientId == patientId)
            .ToListAsync(cancellationToken);

        var entries = new List<PatientHealthTimelineEntryResponse>();
        entries.AddRange(appointments.Select(x => new PatientHealthTimelineEntryResponse(
            "appointment",
            $"{x.Type} appointment with {x.Doctor.User.FullName}",
            $"Status: {x.Status}. Diagnosis: {x.Diagnosis ?? "Pending"}",
            x.Status == Domain.Appointments.AppointmentStatus.Completed ? "Info" : "Primary",
            x.ScheduledAt,
            "Appointment",
            x.Id)));
        entries.AddRange(vitals.Select(x => new PatientHealthTimelineEntryResponse(
            "vitals",
            "Vitals recorded",
            $"BP {x.SystolicBloodPressure}/{x.DiastolicBloodPressure}, Sugar {x.BloodSugarMgDl?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-"}",
            "Info",
            x.RecordedAt,
            "Vitals",
            x.Id)));
        entries.AddRange(labs.Select(x => new PatientHealthTimelineEntryResponse(
            "lab",
            $"Lab booking {x.BookingNumber}",
            $"Status: {x.Status}. Results: {x.Results.Count}",
            x.Results.Any(r => r.HasCriticalValue) ? "Critical" : "Info",
            x.ResultsReleasedAt ?? x.SampleCollectedAt ?? x.CreatedAt,
            "LabBooking",
            x.Id)));
        entries.AddRange(orders.Select(x => new PatientHealthTimelineEntryResponse(
            "pharmacy",
            $"Pharmacy order {x.OrderNumber}",
            $"Status: {x.Status}. Total: {x.TotalAmount:0.00}",
            x.Status == Domain.Pharmacy.PharmacyOrderStatus.Delivered ? "Info" : "Primary",
            x.OrderedAt,
            "PharmacyOrder",
            x.Id)));
        entries.AddRange(dispenses.Select(x => new PatientHealthTimelineEntryResponse(
            "dispense",
            $"Prescription dispensed {x.DispenseNumber}",
            $"Amount: {x.TotalAmount:0.00}",
            "Info",
            x.DispensedAt,
            "PrescriptionDispense",
            x.Id)));

        return entries;
    }

    private static IReadOnlyList<AnalyticsMetricPointResponse> BuildAppointmentSeries(
        DateOnly from,
        DateOnly to,
        IReadOnlyList<Domain.Appointments.Appointment> appointments)
    {
        var points = new List<AnalyticsMetricPointResponse>();
        for (var cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            points.Add(new AnalyticsMetricPointResponse(
                cursor,
                appointments.Count(x => DateOnly.FromDateTime(x.ScheduledAt.UtcDateTime.Date) == cursor)));
        }

        return points;
    }

    private static IReadOnlyList<AnalyticsMetricPointResponse> BuildRevenueSeries(
        DateOnly from,
        DateOnly to,
        IReadOnlyList<Domain.Appointments.Appointment> appointments,
        IReadOnlyList<PaymentTransaction> payments,
        IReadOnlyList<Domain.Labs.LabSampleBooking> labBookings)
    {
        var points = new List<AnalyticsMetricPointResponse>();
        for (var cursor = from; cursor <= to; cursor = cursor.AddDays(1))
        {
            var appointmentRevenue = appointments
                .Where(x => DateOnly.FromDateTime(x.ScheduledAt.UtcDateTime.Date) == cursor && x.Status == Domain.Appointments.AppointmentStatus.Completed)
                .Sum(x => x.ConsultationFee);
            var pharmacyRevenue = payments
                .Where(x => x.PaidAt.HasValue && DateOnly.FromDateTime(x.PaidAt.Value.UtcDateTime.Date) == cursor)
                .Sum(x => x.Amount - x.Refunds.Where(r => r.Status == RefundStatus.Completed).Sum(r => r.Amount));
            var labRevenue = labBookings
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime.Date) == cursor && x.Status != Domain.Labs.LabBookingStatus.Cancelled)
                .Sum(x => x.TotalAmount);

            points.Add(new AnalyticsMetricPointResponse(cursor, appointmentRevenue + pharmacyRevenue + labRevenue));
        }

        return points;
    }

    private static (DateOnly From, DateOnly To) ResolveWindow(DateOnly? from, DateOnly? to)
    {
        var resolvedTo = to ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var resolvedFrom = from ?? resolvedTo.AddDays(-29);
        if (resolvedFrom > resolvedTo)
        {
            (resolvedFrom, resolvedTo) = (resolvedTo, resolvedFrom);
        }

        return (resolvedFrom, resolvedTo);
    }

    private static byte[] GenerateHealthSummaryPdf(
        Domain.Patients.Patient patient,
        IReadOnlyList<PatientHealthTimelineEntryResponse> entries)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(28);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Text("Patient Health Summary").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text(patient.User.FullName);
                    column.Item().Text($"Blood Group: {patient.BloodGroup ?? "Unknown"}");
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Allergies: {patient.MedicalHistory?.Allergies ?? "[]"}");
                    column.Item().Text($"Chronic Diseases: {patient.MedicalHistory?.ChronicDiseases ?? "[]"}");
                    column.Item().Text($"Emergency Contact: {patient.EmergencyContactName} ({patient.EmergencyContactPhone})");
                    foreach (var entry in entries.Take(25))
                    {
                        column.Item().Text($"{entry.OccurredAt:yyyy-MM-dd HH:mm} | {entry.Title} | {entry.Description}");
                    }
                });
            });
        }).GeneratePdf();
    }

    private static byte[] GenerateEmergencyCardPdf(Domain.Patients.Patient patient)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var payload = JsonSerializer.Serialize(new
        {
            patient.Id,
            patient.User.FullName,
            patient.BloodGroup,
            Allergies = patient.MedicalHistory?.Allergies,
            ChronicDiseases = patient.MedicalHistory?.ChronicDiseases,
            patient.EmergencyContactName,
            patient.EmergencyContactPhone
        });
        var qrPng = new PngByteQRCode(new QRCodeGenerator().CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q)).GetGraphic(8);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(16);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Content().Column(column =>
                {
                    column.Item().Text("Emergency QR Card").FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                    column.Item().Text(patient.User.FullName).Bold();
                    column.Item().Text($"Blood Group: {patient.BloodGroup ?? "Unknown"}");
                    column.Item().Text($"Allergies: {patient.MedicalHistory?.Allergies ?? "[]"}");
                    column.Item().Text($"Emergency: {patient.EmergencyContactName} | {patient.EmergencyContactPhone}");
                    column.Item().PaddingTop(8).AlignCenter().Height(120).Image(qrPng);
                });
            });
        }).GeneratePdf();
    }

    private static string BuildAnalyticsEmailBody(AnalyticsSnapshotResponse snapshot)
    {
        return $"""
                HealthCareMS analytics digest
                Window: {snapshot.From:yyyy-MM-dd} to {snapshot.To:yyyy-MM-dd}
                Total appointments: {snapshot.TotalAppointments}
                Completed appointments: {snapshot.CompletedAppointments}
                Appointment revenue: {snapshot.AppointmentRevenue:0.00}
                Pharmacy revenue: {snapshot.PharmacyRevenue:0.00}
                Laboratory revenue: {snapshot.LabRevenue:0.00}
                Total revenue: {snapshot.TotalRevenue:0.00}
                Doctor utilization avg: {snapshot.DoctorUtilizationAveragePercent:0.00}%
                Average lab TAT: {snapshot.AverageLabTurnaroundHours:0.00} hours
                Pharmacy fulfillment rate: {snapshot.PharmacyFulfillmentRatePercent:0.00}%
                """;
    }
}

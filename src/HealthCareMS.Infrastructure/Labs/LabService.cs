using System.Globalization;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Infrastructure.Caching;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HealthCareMS.Infrastructure.Labs;

public sealed class LabService(
    HealthCareDbContext dbContext,
    IDistributedQueryCache queryCache) : ILabService
{
    private const int MinimumCatalogueSize = 120;

    private static readonly LabTest[] EssentialLabTests =
    [
        Test("CBC", "Complete Blood Count", "Hematology", "Blood", 12, null, "No special preparation.", 850m, true, 300m),
        Test("TSH", "Thyroid Stimulating Hormone", "Endocrinology", "Blood", 24, null, "Morning sample preferred.", 1800m, true, 300m),
        Test("HBA1C", "Glycated Hemoglobin HbA1c", "Diabetes", "Blood", 24, null, "No fasting required.", 1600m, true, 300m),
        Test("LFT", "Liver Function Tests", "Biochemistry", "Blood", 24, null, "Avoid heavy meals before sample.", 2200m, true, 300m),
        Test("RFT", "Renal Function Tests", "Biochemistry", "Blood", 24, null, "Hydration advised.", 1900m, true, 300m),
        Test("LIPID", "Lipid Profile", "Cardiology", "Blood", 24, 10, "Fast for 10-12 hours.", 2100m, true, 300m),
        Test("URINE-DR", "Urine Detailed Report", "Pathology", "Urine", 12, null, "Clean-catch sample required.", 650m, false, 0m)
    ];

    public async Task<IReadOnlyList<LabTestResponse>> SearchTestsAsync(string? search, CancellationToken cancellationToken)
    {
        await EnsureDefaultLabTestsAsync(cancellationToken);
        var term = string.IsNullOrWhiteSpace(search) ? "any" : search.Trim().ToLowerInvariant();
        return await queryCache.GetOrCreateAsync(
            "lab-catalog",
            $"tests:{term}",
            TimeSpan.FromMinutes(10),
            async token =>
            {
                var query = dbContext.LabTests.AsNoTracking().Where(x => x.IsActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var localTerm = search.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        x.TestCode.ToLower().Contains(localTerm)
                        || x.TestName.ToLower().Contains(localTerm)
                        || x.Category.ToLower().Contains(localTerm));
                }

                var tests = await query
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.TestName)
                    .Take(200)
                    .ToListAsync(token);

                return (IReadOnlyList<LabTestResponse>)tests.Select(Map).ToList();
            },
            cancellationToken);
    }

    public async Task<Result<LabTestImportResponse>> ImportTestsCsvAsync(
        ImportLabTestsCsvRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CsvContent))
        {
            return Result<LabTestImportResponse>.Failure(Error.Validation([
                new ValidationError(nameof(request.CsvContent), "CsvContent is required.")
            ]));
        }

        var tests = new List<LabTest>();
        var lines = request.CsvContent
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var startIndex = lines.Count > 0 && lines[0].Contains("TestCode", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        for (var index = startIndex; index < lines.Count; index++)
        {
            var columns = SplitCsvLine(lines[index]);
            if (columns.Count < 7)
            {
                return Result<LabTestImportResponse>.Failure(new Error("LAB_TEST_CSV_INVALID", "CSV rows must include TestCode, TestName, Category, SampleType, TurnaroundHours, Price, IsHomeCollectionAvailable."));
            }

            if (!short.TryParse(columns[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var turnaroundHours)
                || !decimal.TryParse(columns[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                || !bool.TryParse(columns[6], out var homeCollection))
            {
                return Result<LabTestImportResponse>.Failure(new Error("LAB_TEST_CSV_INVALID", "CSV turnaround, price, and home collection columns must be valid."));
            }

            short? fastingHours = null;
            if (columns.Count > 7 && !string.IsNullOrWhiteSpace(columns[7]))
            {
                if (!short.TryParse(columns[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedFasting))
                {
                    return Result<LabTestImportResponse>.Failure(new Error("LAB_TEST_CSV_INVALID", "FastingHours must be a valid integer."));
                }

                fastingHours = parsedFasting;
            }

            var preparation = columns.Count > 8 ? Normalize(columns[8]) : null;
            var homeExtra = columns.Count > 9 && decimal.TryParse(columns[9], NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedExtra)
                ? parsedExtra
                : (homeCollection ? 300m : 0m);

            tests.Add(new LabTest
            {
                TenantId = request.TenantId,
                TestCode = columns[0].Trim(),
                TestName = columns[1].Trim(),
                Category = columns[2].Trim(),
                SampleType = columns[3].Trim(),
                TurnaroundHours = turnaroundHours,
                Price = price,
                IsHomeCollectionAvailable = homeCollection,
                FastingHours = fastingHours,
                PreparationInstructions = preparation,
                HomeCollectionExtra = homeExtra,
                IsActive = true
            });
        }

        var validationErrors = tests.SelectMany(ValidateLabTest).ToList();
        if (validationErrors.Count > 0)
        {
            return Result<LabTestImportResponse>.Failure(Error.Validation(validationErrors));
        }

        var codes = tests.Select(x => x.TestCode).ToArray();
        var duplicateExists = await dbContext.LabTests.AnyAsync(x => codes.Contains(x.TestCode), cancellationToken);
        if (duplicateExists || codes.Length != codes.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            return Result<LabTestImportResponse>.Failure(new Error("LAB_TEST_CODE_EXISTS", "CSV contains duplicate or existing lab test codes."));
        }

        dbContext.LabTests.AddRange(tests);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("lab-catalog", cancellationToken);

        return Result<LabTestImportResponse>.Success(new LabTestImportResponse(
            tests.Count,
            tests.Select(Map).ToList()));
    }

    public async Task<Result<IReadOnlyList<LabPanelResponse>>> GetPanelsAsync(
        string? search,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultLabTestsAsync(cancellationToken);

        var term = string.IsNullOrWhiteSpace(search) ? "any" : search.Trim().ToLowerInvariant();
        var panels = await queryCache.GetOrCreateAsync(
            "lab-catalog",
            $"panels:{term}",
            TimeSpan.FromMinutes(10),
            async token =>
            {
                var query = PanelQuery().AsNoTracking().Where(x => x.IsActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var localTerm = search.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        x.PanelCode.ToLower().Contains(localTerm)
                        || x.PanelName.ToLower().Contains(localTerm)
                        || x.Category.ToLower().Contains(localTerm));
                }

                var resolvedPanels = await query
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.PanelName)
                    .Take(100)
                    .ToListAsync(token);

                return (IReadOnlyList<LabPanelResponse>)resolvedPanels.Select(Map).ToList();
            },
            cancellationToken);

        return Result<IReadOnlyList<LabPanelResponse>>.Success(panels);
    }

    public async Task<Result<LabPanelResponse>> CreatePanelAsync(
        CreateLabPanelRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidatePanel(request);
        if (validationErrors.Count > 0)
        {
            return Result<LabPanelResponse>.Failure(Error.Validation(validationErrors));
        }

        await EnsureDefaultLabTestsAsync(cancellationToken);
        var testIds = request.LabTestIds.Distinct().ToArray();
        var tests = await dbContext.LabTests
            .Where(x => testIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
        if (tests.Count != testIds.Length)
        {
            return Result<LabPanelResponse>.Failure(new Error("LAB_PANEL_TEST_NOT_FOUND", "One or more lab panel tests were not found."));
        }

        var panelCode = request.PanelCode.Trim();
        var codeExists = await dbContext.LabPanels.AnyAsync(x => x.PanelCode == panelCode, cancellationToken);
        if (codeExists)
        {
            return Result<LabPanelResponse>.Failure(new Error("LAB_PANEL_CODE_EXISTS", "Lab panel code already exists."));
        }

        var panel = new LabPanel
        {
            TenantId = request.TenantId,
            PanelCode = panelCode,
            PanelName = request.PanelName.Trim(),
            Category = request.Category.Trim(),
            Description = Normalize(request.Description),
            Price = request.Price ?? tests.Sum(x => x.Price),
            IsActive = true
        };

        foreach (var test in tests.OrderBy(x => x.TestName))
        {
            panel.Items.Add(new LabPanelItem
            {
                LabTestId = test.Id,
                LabTest = test
            });
        }

        dbContext.LabPanels.Add(panel);
        await dbContext.SaveChangesAsync(cancellationToken);
        await queryCache.InvalidateNamespaceAsync("lab-catalog", cancellationToken);

        var saved = await PanelQuery().SingleAsync(x => x.Id == panel.Id, cancellationToken);
        return Result<LabPanelResponse>.Success(Map(saved));
    }

    public async Task<Result<LabBookingResponse>> CreateBookingAsync(
        CreateLabBookingRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateBooking(request);
        if (validationErrors.Count > 0)
        {
            return Result<LabBookingResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<LabCollectionType>(request.CollectionType, ignoreCase: true, out var collectionType))
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_TYPE_INVALID", "CollectionType must be OnSite or Home."));
        }

        var patient = await dbContext.Patients
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_PATIENT_NOT_FOUND", "Patient was not found."));
        }

        await EnsureDefaultLabTestsAsync(cancellationToken);
        var testIds = request.LabTestIds.Distinct().ToArray();
        var tests = await dbContext.LabTests
            .Where(x => testIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);
        if (tests.Count != testIds.Length)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_TEST_NOT_FOUND", "One or more lab tests were not found."));
        }

        if (collectionType == LabCollectionType.Home && tests.Any(x => !x.IsHomeCollectionAvailable))
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_HOME_COLLECTION_UNAVAILABLE", "One or more tests are not available for home collection."));
        }

        var now = DateTimeOffset.UtcNow;
        var subTotal = tests.Sum(x => x.Price);
        var homeFee = collectionType == LabCollectionType.Home ? tests.Max(x => x.HomeCollectionExtra) : 0m;
        var booking = new LabSampleBooking
        {
            TenantId = request.TenantId,
            BookingNumber = await GenerateBookingNumberAsync(now, cancellationToken),
            PatientId = patient.Id,
            Patient = patient,
            CollectionType = collectionType,
            Status = LabBookingStatus.Ordered,
            CollectionScheduledAt = request.CollectionScheduledAt?.ToUniversalTime(),
            CollectionAddress = Normalize(request.CollectionAddress),
            SampleBarcode = GenerateSampleBarcode(patient.Id),
            TokenNumber = collectionType == LabCollectionType.OnSite
                ? await GenerateTokenNumberAsync(now, cancellationToken)
                : null,
            Notes = Normalize(request.Notes),
            SubTotal = subTotal,
            HomeCollectionFee = homeFee,
            TotalAmount = subTotal + homeFee
        };

        foreach (var test in tests.OrderBy(x => x.TestName))
        {
            booking.Items.Add(new LabBookingItem
            {
                LabTestId = test.Id,
                LabTest = test,
                Price = test.Price
            });
        }

        dbContext.LabSampleBookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        var saved = await BookingQuery().SingleAsync(x => x.Id == booking.Id, cancellationToken);
        return Result<LabBookingResponse>.Success(Map(saved));
    }

    public async Task<Result<IReadOnlyList<LabBookingResponse>>> GetBookingsAsync(
        string? status,
        string? collectionType,
        DateOnly? date,
        CancellationToken cancellationToken)
    {
        var query = BookingQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<LabBookingStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return Result<IReadOnlyList<LabBookingResponse>>.Failure(new Error("LAB_BOOKING_STATUS_INVALID", "Status is invalid."));
            }

            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(collectionType))
        {
            if (!Enum.TryParse<LabCollectionType>(collectionType, ignoreCase: true, out var parsedCollectionType))
            {
                return Result<IReadOnlyList<LabBookingResponse>>.Failure(new Error("LAB_COLLECTION_TYPE_INVALID", "CollectionType must be OnSite or Home."));
            }

            query = query.Where(x => x.CollectionType == parsedCollectionType);
        }

        if (date.HasValue)
        {
            var dayStart = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
        }

        var bookings = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LabBookingResponse>>.Success(bookings.Select(Map).ToList());
    }

    public async Task<Result<LabBookingResponse>> CheckInBookingAsync(
        Guid bookingId,
        CheckInLabBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        if (booking.CollectionType != LabCollectionType.OnSite)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_CHECKIN_INVALID", "Only onsite bookings can be checked in."));
        }

        if (booking.Status != LabBookingStatus.Ordered)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_BOOKING_STATUS_INVALID", "Only ordered bookings can be checked in."));
        }

        booking.Status = LabBookingStatus.CheckedIn;
        booking.FastingVerified = request.FastingVerified;
        booking.CheckedInAt = DateTimeOffset.UtcNow;
        booking.Notes = MergeNotes(booking.Notes, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabBookingResponse>.Success(Map(booking));
    }

    public async Task<Result<LabBarcodeLabelPdfResponse>> GenerateBarcodeLabelPdfAsync(
        Guid bookingId,
        CancellationToken cancellationToken)
    {
        var booking = await BookingQuery().SingleOrDefaultAsync(x => x.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            return Result<LabBarcodeLabelPdfResponse>.Failure(new Error("LAB_BOOKING_NOT_FOUND", "Lab booking was not found."));
        }

        booking.BarcodeLabelGeneratedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var pdf = GenerateBarcodeLabelPdf(booking);
        return Result<LabBarcodeLabelPdfResponse>.Success(new LabBarcodeLabelPdfResponse(
            pdf,
            $"{booking.BookingNumber}-Label.pdf",
            "application/pdf"));
    }

    public async Task<Result<LabBookingResponse>> CreateConsultationLabOrderAsync(
        Guid appointmentId,
        CreateConsultationLabOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateOrder(request);
        if (validationErrors.Count > 0)
        {
            return Result<LabBookingResponse>.Failure(Error.Validation(validationErrors));
        }

        if (!Enum.TryParse<LabCollectionType>(request.CollectionType, ignoreCase: true, out var collectionType))
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_COLLECTION_TYPE_INVALID", "CollectionType must be OnSite or Home."));
        }

        var appointment = await dbContext.Appointments
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_APPOINTMENT_INVALID", "Lab order cannot be created for cancelled or no-show appointments."));
        }

        await EnsureDefaultLabTestsAsync(cancellationToken);
        var testIds = request.LabTestIds.Distinct().ToArray();
        var tests = await dbContext.LabTests
            .Where(x => testIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (tests.Count != testIds.Length)
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_TEST_NOT_FOUND", "One or more lab tests were not found."));
        }

        if (collectionType == LabCollectionType.Home && tests.Any(x => !x.IsHomeCollectionAvailable))
        {
            return Result<LabBookingResponse>.Failure(new Error("LAB_HOME_COLLECTION_UNAVAILABLE", "One or more tests are not available for home collection."));
        }

        var prescriptionId = await dbContext.Prescriptions
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var subTotal = tests.Sum(x => x.Price);
        var homeFee = collectionType == LabCollectionType.Home ? tests.Max(x => x.HomeCollectionExtra) : 0m;
        var booking = new LabSampleBooking
        {
            BookingNumber = await GenerateBookingNumberAsync(DateTimeOffset.UtcNow, cancellationToken),
            PatientId = appointment.PatientId,
            Patient = appointment.Patient,
            AppointmentId = appointment.Id,
            Appointment = appointment,
            PrescriptionId = prescriptionId,
            CollectionType = collectionType,
            Status = LabBookingStatus.Ordered,
            CollectionScheduledAt = request.CollectionScheduledAt?.ToUniversalTime(),
            CollectionAddress = Normalize(request.CollectionAddress),
            SampleBarcode = GenerateSampleBarcode(appointmentId),
            TokenNumber = collectionType == LabCollectionType.OnSite
                ? await GenerateTokenNumberAsync(DateTimeOffset.UtcNow, cancellationToken)
                : null,
            Notes = Normalize(request.Notes),
            SubTotal = subTotal,
            HomeCollectionFee = homeFee,
            TotalAmount = subTotal + homeFee
        };

        foreach (var test in tests.OrderBy(x => x.TestName))
        {
            booking.Items.Add(new LabBookingItem
            {
                LabTestId = test.Id,
                LabTest = test,
                Price = test.Price
            });
        }

        dbContext.LabSampleBookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<LabBookingResponse>.Success(Map(booking));
    }

    public async Task<Result<IReadOnlyList<LabBookingResponse>>> GetBookingsByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var appointmentExists = await dbContext.Appointments.AnyAsync(x => x.Id == appointmentId, cancellationToken);
        if (!appointmentExists)
        {
            return Result<IReadOnlyList<LabBookingResponse>>.Failure(new Error("LAB_APPOINTMENT_NOT_FOUND", "Appointment was not found."));
        }

        var bookings = await BookingQuery()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<LabBookingResponse>>.Success(bookings.Select(Map).ToList());
    }

    internal IQueryable<LabSampleBooking> BookingQuery()
    {
        return dbContext.LabSampleBookings
            .Include(x => x.Patient)
            .ThenInclude(x => x.User)
            .Include(x => x.Items)
            .ThenInclude(x => x.LabTest);
    }

    private IQueryable<LabPanel> PanelQuery()
    {
        return dbContext.LabPanels
            .Include(x => x.Items)
            .ThenInclude(x => x.LabTest);
    }

    private async Task EnsureDefaultLabTestsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await dbContext.LabTests
            .Select(x => x.TestCode)
            .ToListAsync(cancellationToken);
        if (existingCodes.Count >= MinimumCatalogueSize)
        {
            return;
        }

        var existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var catalogue = BuildDefaultCatalogue()
            .Where(x => !existing.Contains(x.TestCode))
            .Take(MinimumCatalogueSize - existingCodes.Count)
            .Select(Clone)
            .ToList();
        if (catalogue.Count == 0)
        {
            return;
        }

        dbContext.LabTests.AddRange(catalogue);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateBookingNumberAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var datePart = createdAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"LAB-{datePart}-";
        var count = await dbContext.LabSampleBookings.CountAsync(x => x.BookingNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
    }

    private async Task<string> GenerateTokenNumberAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var datePart = createdAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"LQ-{datePart}-";
        var count = await dbContext.LabSampleBookings.CountAsync(x => x.TokenNumber != null && x.TokenNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000}";
    }

    private static List<ValidationError> ValidateOrder(CreateConsultationLabOrderRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.LabTestIds is null || request.LabTestIds.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.LabTestIds), "At least one lab test is required."));
        }

        if (string.IsNullOrWhiteSpace(request.CollectionType))
        {
            errors.Add(new ValidationError(nameof(request.CollectionType), "CollectionType is required."));
        }

        if (!string.IsNullOrWhiteSpace(request.CollectionAddress) && request.CollectionAddress.Trim().Length > 4000)
        {
            errors.Add(new ValidationError(nameof(request.CollectionAddress), "CollectionAddress cannot exceed 4000 characters."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateBooking(CreateLabBookingRequest request)
    {
        var errors = new List<ValidationError>();
        if (request.PatientId == Guid.Empty)
        {
            errors.Add(new ValidationError(nameof(request.PatientId), "PatientId is required."));
        }

        if (request.LabTestIds is null || request.LabTestIds.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.LabTestIds), "At least one lab test is required."));
        }

        Required(request.CollectionType, nameof(request.CollectionType), errors);
        if (!string.IsNullOrWhiteSpace(request.CollectionAddress) && request.CollectionAddress.Trim().Length > 4000)
        {
            errors.Add(new ValidationError(nameof(request.CollectionAddress), "CollectionAddress cannot exceed 4000 characters."));
        }

        if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.Notes), "Notes cannot exceed 1000 characters."));
        }

        return errors;
    }

    private static List<ValidationError> ValidateLabTest(LabTest test)
    {
        var errors = new List<ValidationError>();
        Required(test.TestCode, nameof(test.TestCode), errors);
        Required(test.TestName, nameof(test.TestName), errors);
        Required(test.Category, nameof(test.Category), errors);
        Required(test.SampleType, nameof(test.SampleType), errors);
        if (test.TurnaroundHours <= 0)
        {
            errors.Add(new ValidationError(nameof(test.TurnaroundHours), "TurnaroundHours must be positive."));
        }

        if (test.Price < 0)
        {
            errors.Add(new ValidationError(nameof(test.Price), "Price cannot be negative."));
        }

        if (test.HomeCollectionExtra < 0)
        {
            errors.Add(new ValidationError(nameof(test.HomeCollectionExtra), "HomeCollectionExtra cannot be negative."));
        }

        return errors;
    }

    private static List<ValidationError> ValidatePanel(CreateLabPanelRequest request)
    {
        var errors = new List<ValidationError>();
        Required(request.PanelCode, nameof(request.PanelCode), errors);
        Required(request.PanelName, nameof(request.PanelName), errors);
        Required(request.Category, nameof(request.Category), errors);
        if (request.LabTestIds is null || request.LabTestIds.Count == 0)
        {
            errors.Add(new ValidationError(nameof(request.LabTestIds), "At least one lab test is required."));
        }

        if (request.Price is < 0)
        {
            errors.Add(new ValidationError(nameof(request.Price), "Price cannot be negative."));
        }

        if (!string.IsNullOrWhiteSpace(request.Description) && request.Description.Trim().Length > 1000)
        {
            errors.Add(new ValidationError(nameof(request.Description), "Description cannot exceed 1000 characters."));
        }

        return errors;
    }

    private static IReadOnlyList<LabTest> BuildDefaultCatalogue()
    {
        var tests = EssentialLabTests.Select(Clone).ToList();
        var categories = new[]
        {
            ("HEM", "Hematology", "Blood"),
            ("BIO", "Biochemistry", "Blood"),
            ("END", "Endocrinology", "Blood"),
            ("MIC", "Microbiology", "Swab"),
            ("IMM", "Immunology", "Blood"),
            ("CAR", "Cardiology", "Blood"),
            ("REN", "Renal", "Blood"),
            ("LIV", "Liver", "Blood"),
            ("URI", "Urinalysis", "Urine"),
            ("MOL", "Molecular", "Blood")
        };
        var names = new[]
        {
            "Screen",
            "Profile",
            "Quantitative Assay",
            "Rapid Test",
            "Antibody Panel",
            "Antigen Test",
            "Culture",
            "Sensitivity",
            "Electrolyte Check",
            "Enzyme Level",
            "Marker",
            "Confirmatory Test"
        };

        var sequence = 1;
        foreach (var category in categories)
        {
            foreach (var name in names)
            {
                tests.Add(Test(
                    $"{category.Item1}-{sequence:000}",
                    $"{category.Item2} {name}",
                    category.Item2,
                    category.Item3,
                    (short)(sequence % 4 == 0 ? 48 : sequence % 3 == 0 ? 36 : 24),
                    sequence % 5 == 0 ? (short)8 : null,
                    sequence % 5 == 0 ? "Fast for 8 hours." : "No special preparation.",
                    600m + (sequence * 35m),
                    category.Item3 is "Blood" or "Urine",
                    category.Item3 is "Blood" or "Urine" ? 300m : 0m));
                sequence++;
            }
        }

        return tests
            .GroupBy(x => x.TestCode, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(MinimumCatalogueSize)
            .ToList();
    }

    private static LabTest Test(
        string code,
        string name,
        string category,
        string sampleType,
        short turnaroundHours,
        short? fastingHours,
        string? preparation,
        decimal price,
        bool homeCollection,
        decimal homeExtra)
    {
        return new LabTest
        {
            TestCode = code,
            TestName = name,
            Category = category,
            SampleType = sampleType,
            TurnaroundHours = turnaroundHours,
            FastingHours = fastingHours,
            PreparationInstructions = preparation,
            Price = price,
            IsHomeCollectionAvailable = homeCollection,
            HomeCollectionExtra = homeExtra,
            IsActive = true
        };
    }

    private static LabTest Clone(LabTest source)
    {
        return Test(
            source.TestCode,
            source.TestName,
            source.Category,
            source.SampleType,
            source.TurnaroundHours,
            source.FastingHours,
            source.PreparationInstructions,
            source.Price,
            source.IsHomeCollectionAvailable,
            source.HomeCollectionExtra);
    }

    private static LabTestResponse Map(LabTest test)
    {
        return new LabTestResponse(
            test.Id,
            test.TenantId,
            test.TestCode,
            test.TestName,
            test.Category,
            test.SampleType,
            test.TurnaroundHours,
            test.FastingHours,
            test.PreparationInstructions,
            test.Price,
            test.IsHomeCollectionAvailable,
            test.HomeCollectionExtra,
            test.IsActive);
    }

    private static LabBookingResponse Map(LabSampleBooking booking)
    {
        return new LabBookingResponse(
            booking.Id,
            booking.BookingNumber,
            booking.TenantId,
            booking.PatientId,
            $"{booking.Patient.FirstName} {booking.Patient.LastName}".Trim(),
            booking.AppointmentId,
            booking.PrescriptionId,
            booking.CollectionType.ToString(),
            booking.Status.ToString(),
            booking.CollectionScheduledAt,
            booking.CollectionAddress,
            booking.SampleBarcode,
            booking.TokenNumber,
            booking.FastingVerified,
            booking.CheckedInAt,
            booking.BarcodeLabelGeneratedAt,
            booking.Notes,
            booking.SubTotal,
            booking.HomeCollectionFee,
            booking.TotalAmount,
            booking.CreatedAt,
            booking.Items
                .OrderBy(x => x.LabTest.TestName)
                .Select(Map)
                .ToList());
    }

    private static LabBookingItemResponse Map(LabBookingItem item)
    {
        return new LabBookingItemResponse(
            item.Id,
            item.LabTestId,
            item.LabTest.TestCode,
            item.LabTest.TestName,
            item.LabTest.Category,
            item.Price);
    }

    private static LabPanelResponse Map(LabPanel panel)
    {
        return new LabPanelResponse(
            panel.Id,
            panel.TenantId,
            panel.PanelCode,
            panel.PanelName,
            panel.Category,
            panel.Description,
            panel.Price,
            panel.IsActive,
            panel.Items
                .OrderBy(x => x.LabTest.TestName)
                .Select(x => Map(x.LabTest))
                .ToList());
    }

    private static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Add('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()).Trim());
                current.Clear();
            }
            else
            {
                current.Add(ch);
            }
        }

        values.Add(new string(current.ToArray()).Trim());
        return values;
    }

    private static string GenerateSampleBarcode(Guid appointmentId)
    {
        return $"SMP-{DateTimeOffset.UtcNow:yyyyMMdd}-{appointmentId.ToString("N")[..8].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static byte[] GenerateBarcodeLabelPdf(LabSampleBooking booking)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(18);
                page.DefaultTextStyle(text => text.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Column(column =>
                {
                    column.Item().Text("HealthCareMS Lab Label").FontSize(15).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Booking: {booking.BookingNumber}");
                    if (!string.IsNullOrWhiteSpace(booking.TokenNumber))
                    {
                        column.Item().Text($"Token: {booking.TokenNumber}").Bold();
                    }
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Text($"{booking.Patient.FirstName} {booking.Patient.LastName}".Trim()).Bold();
                    column.Item().Text($"Barcode: {booking.SampleBarcode}").FontSize(12).Bold();
                    column.Item().Text($"Collection: {booking.CollectionType}");
                    column.Item().Text($"Fasting verified: {(booking.FastingVerified == true ? "Yes" : "No")}");
                    column.Item().Text("Tests").Bold();
                    foreach (var item in booking.Items.OrderBy(x => x.LabTest.TestName))
                    {
                        column.Item().Text($"{item.LabTest.TestCode} - {item.LabTest.TestName}");
                    }
                });

                page.Footer().AlignCenter().Text($"Generated {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
            });
        }).GeneratePdf();
    }

    private static string? MergeNotes(string? existingNotes, string? newNotes)
    {
        var normalized = Normalize(newNotes);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return existingNotes;
        }

        return string.IsNullOrWhiteSpace(existingNotes)
            ? normalized
            : $"{existingNotes.Trim()} | {normalized}";
    }

    private static void Required(string? value, string field, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(field, $"{field} is required."));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Appointments;
using HealthCareMS.Domain.Labs;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthCareMS.Infrastructure.Labs;

public sealed class LabService(HealthCareDbContext dbContext) : ILabService
{
    private static readonly LabTest[] DefaultLabTests =
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

        var query = dbContext.LabTests.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.TestCode.ToLower().Contains(term)
                || x.TestName.ToLower().Contains(term)
                || x.Category.ToLower().Contains(term));
        }

        var tests = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.TestName)
            .Take(100)
            .ToListAsync(cancellationToken);

        return tests.Select(Map).ToList();
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

    private async Task EnsureDefaultLabTestsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.LabTests.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.LabTests.AddRange(DefaultLabTests.Select(Clone));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateBookingNumberAsync(DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var datePart = createdAt.UtcDateTime.ToString("yyyyMMdd");
        var prefix = $"LAB-{datePart}-";
        var count = await dbContext.LabSampleBookings.CountAsync(x => x.BookingNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:000000}";
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

    private static string GenerateSampleBarcode(Guid appointmentId)
    {
        return $"SMP-{DateTimeOffset.UtcNow:yyyyMMdd}-{appointmentId.ToString("N")[..8].ToUpperInvariant()}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

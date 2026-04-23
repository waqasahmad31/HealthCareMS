namespace HealthCareMS.Application.Labs;

public sealed record LabTestResponse(
    Guid Id,
    Guid? TenantId,
    string TestCode,
    string TestName,
    string Category,
    string SampleType,
    short TurnaroundHours,
    short? FastingHours,
    string? PreparationInstructions,
    decimal Price,
    bool IsHomeCollectionAvailable,
    decimal HomeCollectionExtra,
    bool IsActive);

public sealed record LabBookingResponse(
    Guid Id,
    string BookingNumber,
    Guid? TenantId,
    Guid PatientId,
    string PatientName,
    Guid? AppointmentId,
    Guid? PrescriptionId,
    string CollectionType,
    string Status,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? SampleBarcode,
    string? Notes,
    decimal SubTotal,
    decimal HomeCollectionFee,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LabBookingItemResponse> Items);

public sealed record LabBookingItemResponse(
    Guid Id,
    Guid LabTestId,
    string TestCode,
    string TestName,
    string Category,
    decimal Price);

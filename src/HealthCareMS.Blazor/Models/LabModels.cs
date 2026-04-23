namespace HealthCareMS.Blazor.Models;

public sealed record LabTestModel(
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

public sealed record CreateConsultationLabOrderModel(
    IReadOnlyList<Guid> LabTestIds,
    string CollectionType,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? Notes);

public sealed record LabBookingModel(
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
    IReadOnlyList<LabBookingItemModel> Items);

public sealed record LabBookingItemModel(
    Guid Id,
    Guid LabTestId,
    string TestCode,
    string TestName,
    string Category,
    decimal Price);

public sealed record ConsultationSummaryModel(
    Guid AppointmentId,
    string AppointmentNumber,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    string Status,
    string? Diagnosis,
    string? Icd10Code,
    string? Icd10Title,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    PrescriptionResultModel? Prescription,
    IReadOnlyList<LabBookingModel> LabOrders);

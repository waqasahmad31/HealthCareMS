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
    string? TokenNumber,
    bool? FastingVerified,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? BarcodeLabelGeneratedAt,
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

public sealed record LabTestImportModel(
    int ImportedCount,
    IReadOnlyList<LabTestModel> Tests);

public sealed class LabPanelFormModel
{
    public Guid? TenantId { get; set; }

    public string PanelCode { get; set; } = string.Empty;

    public string PanelName { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public IReadOnlyList<Guid> LabTestIds { get; set; } = [];
}

public sealed record LabPanelModel(
    Guid Id,
    Guid? TenantId,
    string PanelCode,
    string PanelName,
    string Category,
    string? Description,
    decimal Price,
    bool IsActive,
    IReadOnlyList<LabTestModel> Tests);

public sealed class LabBookingFormModel
{
    public Guid? TenantId { get; set; }

    public Guid PatientId { get; set; }

    public IReadOnlyList<Guid> LabTestIds { get; set; } = [];

    public string CollectionType { get; set; } = "OnSite";

    public DateTimeOffset? CollectionScheduledAt { get; set; }

    public string? CollectionAddress { get; set; }

    public string? Notes { get; set; }
}

public sealed class LabCheckInFormModel
{
    public bool FastingVerified { get; set; } = true;

    public string? Notes { get; set; }
}

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

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
    string? Notes,
    DateTimeOffset? CollectionWindowEndAt);

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
    IReadOnlyList<LabBookingItemModel> Items,
    DateTimeOffset? CollectionWindowEndAt,
    Guid? CollectionAgentUserId,
    string? CollectionAgentName,
    DateTimeOffset? CollectionAssignedAt,
    DateTimeOffset? CollectionStartedAt,
    DateTimeOffset? SampleCollectedAt,
    DateTimeOffset? ResultsReleasedAt,
    DateTimeOffset? ReportGeneratedAt,
    string? ReportVerificationCode);

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

    public DateTimeOffset? CollectionWindowEndAt { get; set; }

    public string? CollectionAddress { get; set; }

    public string? Notes { get; set; }
}

public sealed class LabCheckInFormModel
{
    public bool FastingVerified { get; set; } = true;

    public string? Notes { get; set; }
}

public sealed class AssignLabCollectionAgentFormModel
{
    public Guid CollectionAgentUserId { get; set; }

    public DateTimeOffset? CollectionScheduledAt { get; set; }

    public DateTimeOffset? CollectionWindowEndAt { get; set; }

    public string? Notes { get; set; }
}

public sealed class StartLabCollectionFormModel
{
    public string? Notes { get; set; }
}

public sealed class MarkLabSampleCollectedFormModel
{
    public bool? FastingVerified { get; set; }

    public string? Notes { get; set; }
}

public sealed class LabResultParameterFormModel
{
    public string ParameterName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal? ReferenceLow { get; set; }

    public decimal? ReferenceHigh { get; set; }

    public string? ReferenceText { get; set; }

    public decimal? CriticalLow { get; set; }

    public decimal? CriticalHigh { get; set; }

    public string? Notes { get; set; }
}

public sealed class UpsertLabTestResultFormModel
{
    public Guid LabBookingItemId { get; set; }

    public string? Summary { get; set; }

    public IReadOnlyList<LabResultParameterFormModel> Parameters { get; set; } = [];
}

public sealed class EnterLabResultsFormModel
{
    public IReadOnlyList<UpsertLabTestResultFormModel> Results { get; set; } = [];
}

public sealed class AcknowledgeLabCriticalAlertFormModel
{
    public string? AcknowledgementNote { get; set; }
}

public sealed class ValidateLabResultsFormModel
{
    public string Level { get; set; } = "Tech";

    public string? Notes { get; set; }
}

public sealed class ReleaseLabResultsFormModel
{
    public string? Notes { get; set; }
}

public sealed class AddLabResultAddendumFormModel
{
    public string AddendumNotes { get; set; } = string.Empty;
}

public sealed record LabResultParameterModel(
    string ParameterName,
    string Value,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    bool IsAbnormal,
    bool IsCritical,
    string? Notes);

public sealed record LabTestResultModel(
    Guid Id,
    Guid LabSampleBookingId,
    Guid LabBookingItemId,
    Guid LabTestId,
    string ResultNumber,
    string TestCode,
    string TestName,
    string Status,
    string? Summary,
    bool IsAbnormal,
    bool HasCriticalValue,
    string? CriticalValueSummary,
    DateTimeOffset? EnteredAt,
    DateTimeOffset? TechnicianValidatedAt,
    DateTimeOffset? ManagerValidatedAt,
    DateTimeOffset? ReleasedAt,
    DateTimeOffset? CriticalAlertSentAt,
    DateTimeOffset? CriticalAlertAcknowledgedAt,
    string? AddendumNotes,
    DateTimeOffset? AddendumAt,
    IReadOnlyList<LabResultParameterModel> Parameters);

public sealed record LabValidationQueueItemModel(
    Guid BookingId,
    string BookingNumber,
    Guid PatientId,
    string PatientName,
    string BookingStatus,
    bool HasCriticalValue,
    bool HasAbnormalResult,
    int PendingTechValidationCount,
    int PendingManagerValidationCount,
    int ReleasableCount,
    DateTimeOffset CreatedAt);

public sealed record LabBookingResultSummaryModel(
    Guid BookingId,
    string BookingNumber,
    string PatientName,
    string Status,
    DateTimeOffset? ResultsReleasedAt,
    string? ReportVerificationCode,
    IReadOnlyList<LabTestResultModel> Results);

public sealed record LabReportVerificationModel(
    Guid BookingId,
    string BookingNumber,
    string PatientName,
    bool IsVerified,
    string Status,
    DateTimeOffset? ReleasedAt,
    IReadOnlyList<string> TestNames);

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

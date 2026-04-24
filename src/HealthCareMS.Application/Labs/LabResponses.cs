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
    string? TokenNumber,
    bool? FastingVerified,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? BarcodeLabelGeneratedAt,
    string? Notes,
    decimal SubTotal,
    decimal HomeCollectionFee,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<LabBookingItemResponse> Items,
    DateTimeOffset? CollectionWindowEndAt,
    Guid? CollectionAgentUserId,
    string? CollectionAgentName,
    DateTimeOffset? CollectionAssignedAt,
    DateTimeOffset? CollectionStartedAt,
    DateTimeOffset? SampleCollectedAt,
    DateTimeOffset? ResultsReleasedAt,
    DateTimeOffset? ReportGeneratedAt,
    string? ReportVerificationCode);

public sealed record LabBookingItemResponse(
    Guid Id,
    Guid LabTestId,
    string TestCode,
    string TestName,
    string Category,
    decimal Price);

public sealed record LabTestImportResponse(
    int ImportedCount,
    IReadOnlyList<LabTestResponse> Tests);

public sealed record LabPanelResponse(
    Guid Id,
    Guid? TenantId,
    string PanelCode,
    string PanelName,
    string Category,
    string? Description,
    decimal Price,
    bool IsActive,
    IReadOnlyList<LabTestResponse> Tests);

public sealed record LabBarcodeLabelPdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed record LabResultParameterResponse(
    string ParameterName,
    string Value,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    bool IsAbnormal,
    bool IsCritical,
    string? Notes);

public sealed record LabTestResultResponse(
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
    IReadOnlyList<LabResultParameterResponse> Parameters);

public sealed record LabValidationQueueItemResponse(
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

public sealed record LabReportPdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed record LabBookingResultSummaryResponse(
    Guid BookingId,
    string BookingNumber,
    string PatientName,
    string Status,
    DateTimeOffset? ResultsReleasedAt,
    string? ReportVerificationCode,
    IReadOnlyList<LabTestResultResponse> Results);

public sealed record LabReportVerificationResponse(
    Guid BookingId,
    string BookingNumber,
    string PatientName,
    bool IsVerified,
    string Status,
    DateTimeOffset? ReleasedAt,
    IReadOnlyList<string> TestNames);

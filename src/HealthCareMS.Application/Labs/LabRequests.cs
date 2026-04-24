namespace HealthCareMS.Application.Labs;

public sealed record CreateConsultationLabOrderRequest(
    IReadOnlyList<Guid> LabTestIds,
    string CollectionType,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? Notes,
    DateTimeOffset? CollectionWindowEndAt = null);

public sealed record ImportLabTestsCsvRequest(Guid? TenantId, string CsvContent);

public sealed record CreateLabPanelRequest(
    Guid? TenantId,
    string PanelCode,
    string PanelName,
    string Category,
    string? Description,
    decimal? Price,
    IReadOnlyList<Guid> LabTestIds);

public sealed record CreateLabBookingRequest(
    Guid? TenantId,
    Guid PatientId,
    IReadOnlyList<Guid> LabTestIds,
    string CollectionType,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? Notes,
    DateTimeOffset? CollectionWindowEndAt = null);

public sealed record CheckInLabBookingRequest(
    bool FastingVerified,
    string? Notes);

public sealed record AssignLabCollectionAgentRequest(
    Guid CollectionAgentUserId,
    DateTimeOffset? CollectionScheduledAt,
    DateTimeOffset? CollectionWindowEndAt,
    string? Notes);

public sealed record StartLabCollectionRequest(
    string? Notes);

public sealed record MarkLabSampleCollectedRequest(
    bool? FastingVerified,
    string? Notes);

public sealed record LabResultParameterRequest(
    string ParameterName,
    string Value,
    string? Unit,
    decimal? ReferenceLow,
    decimal? ReferenceHigh,
    string? ReferenceText,
    decimal? CriticalLow,
    decimal? CriticalHigh,
    string? Notes);

public sealed record UpsertLabTestResultRequest(
    Guid LabBookingItemId,
    string? Summary,
    IReadOnlyList<LabResultParameterRequest> Parameters);

public sealed record EnterLabResultsRequest(
    IReadOnlyList<UpsertLabTestResultRequest> Results);

public sealed record AcknowledgeLabCriticalAlertRequest(
    string? AcknowledgementNote);

public sealed record ValidateLabResultsRequest(
    string Level,
    string? Notes);

public sealed record ReleaseLabResultsRequest(
    string? Notes);

public sealed record AddLabResultAddendumRequest(
    string AddendumNotes);

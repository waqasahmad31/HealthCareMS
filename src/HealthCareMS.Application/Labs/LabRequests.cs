namespace HealthCareMS.Application.Labs;

public sealed record CreateConsultationLabOrderRequest(
    IReadOnlyList<Guid> LabTestIds,
    string CollectionType,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? Notes);

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
    string? Notes);

public sealed record CheckInLabBookingRequest(
    bool FastingVerified,
    string? Notes);

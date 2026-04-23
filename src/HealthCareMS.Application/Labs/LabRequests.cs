namespace HealthCareMS.Application.Labs;

public sealed record CreateConsultationLabOrderRequest(
    IReadOnlyList<Guid> LabTestIds,
    string CollectionType,
    DateTimeOffset? CollectionScheduledAt,
    string? CollectionAddress,
    string? Notes);

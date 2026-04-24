using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Insights;

public interface IInsightService
{
    Task<Result<PatientHealthTimelineResponse>> GetPatientTimelineAsync(
        Guid patientId,
        string? filter,
        CancellationToken cancellationToken);

    Task<Result<InsightDocumentResponse>> GenerateHealthSummaryPdfAsync(
        Guid patientId,
        CancellationToken cancellationToken);

    Task<Result<InsightDocumentResponse>> GenerateEmergencyCardPdfAsync(
        Guid patientId,
        CancellationToken cancellationToken);

    Task<Result<AnalyticsSnapshotResponse>> GetAnalyticsAsync(
        Guid? tenantId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task SendScheduledAnalyticsEmailsAsync(CancellationToken cancellationToken);
}

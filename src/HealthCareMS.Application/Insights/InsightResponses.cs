namespace HealthCareMS.Application.Insights;

public sealed record PatientHealthTimelineEntryResponse(
    string Type,
    string Title,
    string Description,
    string Severity,
    DateTimeOffset OccurredAt,
    string ReferenceType,
    Guid? ReferenceId);

public sealed record PatientHealthTimelineResponse(
    Guid PatientId,
    string PatientName,
    string? BloodGroup,
    IReadOnlyList<PatientHealthTimelineEntryResponse> Entries);

public sealed record InsightDocumentResponse(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed record AnalyticsMetricPointResponse(
    DateOnly Date,
    decimal Value);

public sealed record ModuleRevenueResponse(
    string Module,
    decimal Revenue);

public sealed record DoctorUtilizationResponse(
    Guid DoctorId,
    string DoctorName,
    decimal UtilizationPercent,
    int AppointmentCount,
    decimal Revenue);

public sealed record AnalyticsSnapshotResponse(
    DateOnly From,
    DateOnly To,
    int TotalAppointments,
    int CompletedAppointments,
    decimal AppointmentRevenue,
    decimal PharmacyRevenue,
    decimal LabRevenue,
    decimal TotalRevenue,
    decimal DoctorUtilizationAveragePercent,
    decimal AverageLabTurnaroundHours,
    decimal PharmacyFulfillmentRatePercent,
    IReadOnlyList<AnalyticsMetricPointResponse> AppointmentSeries,
    IReadOnlyList<AnalyticsMetricPointResponse> RevenueSeries,
    IReadOnlyList<ModuleRevenueResponse> ModuleRevenue,
    IReadOnlyList<DoctorUtilizationResponse> DoctorUtilization);

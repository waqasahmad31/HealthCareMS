namespace HealthCareMS.Blazor.Models;

public sealed record PatientHealthTimelineEntryModel(
    string Type,
    string Title,
    string Description,
    string Severity,
    DateTimeOffset OccurredAt,
    string ReferenceType,
    Guid? ReferenceId);

public sealed record PatientHealthTimelineModel(
    Guid PatientId,
    string PatientName,
    string? BloodGroup,
    IReadOnlyList<PatientHealthTimelineEntryModel> Entries);

public sealed record AnalyticsMetricPointModel(
    DateOnly Date,
    decimal Value);

public sealed record ModuleRevenueModel(
    string Module,
    decimal Revenue);

public sealed record DoctorUtilizationModel(
    Guid DoctorId,
    string DoctorName,
    decimal UtilizationPercent,
    int AppointmentCount,
    decimal Revenue);

public sealed record AnalyticsSnapshotModel(
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
    IReadOnlyList<AnalyticsMetricPointModel> AppointmentSeries,
    IReadOnlyList<AnalyticsMetricPointModel> RevenueSeries,
    IReadOnlyList<ModuleRevenueModel> ModuleRevenue,
    IReadOnlyList<DoctorUtilizationModel> DoctorUtilization);

namespace HealthCareMS.Blazor.Models;

public sealed class RecordVitalsModel
{
    public DateTimeOffset? RecordedAt { get; set; }

    public int? SystolicBloodPressure { get; set; }

    public int? DiastolicBloodPressure { get; set; }

    public short? HeartRate { get; set; }

    public decimal? BloodSugarMgDl { get; set; }

    public string? BloodSugarContext { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public string? Notes { get; set; }
}

public sealed record PatientVitalsModel(
    Guid Id,
    Guid PatientId,
    DateTimeOffset RecordedAt,
    int? SystolicBloodPressure,
    int? DiastolicBloodPressure,
    short? HeartRate,
    decimal? BloodSugarMgDl,
    string? BloodSugarContext,
    decimal? WeightKg,
    decimal? TemperatureCelsius,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record VitalTrendModel(
    string Metric,
    string Unit,
    decimal? LatestValue,
    decimal? PreviousValue,
    decimal? Change,
    string Direction,
    DateTimeOffset? LatestRecordedAt);

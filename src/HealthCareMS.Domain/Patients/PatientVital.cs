using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Patients;

public sealed class PatientVital : BaseEntity
{
    public Guid PatientId { get; set; }

    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;

    public int? SystolicBloodPressure { get; set; }

    public int? DiastolicBloodPressure { get; set; }

    public short? HeartRate { get; set; }

    public decimal? BloodSugarMgDl { get; set; }

    public string? BloodSugarContext { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? TemperatureCelsius { get; set; }

    public string? Notes { get; set; }

    public Patient Patient { get; set; } = null!;
}

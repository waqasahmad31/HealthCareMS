using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Consultations;

public sealed class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; set; }

    public short SortOrder { get; set; }

    public string MedicineName { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public string? Strength { get; set; }

    public string? Route { get; set; }

    public string Dosage { get; set; } = string.Empty;

    public string Frequency { get; set; } = string.Empty;

    public short DurationDays { get; set; }

    public decimal Quantity { get; set; }

    public string? Instructions { get; set; }

    public bool IsSubstitutionAllowed { get; set; } = true;

    public Prescription Prescription { get; set; } = null!;
}

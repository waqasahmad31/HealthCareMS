using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Consultations;

public sealed class DrapMedicine : BaseEntity
{
    public string DrapRegistrationNumber { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string GenericName { get; set; } = string.Empty;

    public string? Strength { get; set; }

    public string DosageForm { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public string AllergenKeywords { get; set; } = string.Empty;

    public bool IsBanned { get; set; }
}

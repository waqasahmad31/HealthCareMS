using HealthCareMS.Domain.Common;

namespace HealthCareMS.Domain.Patients;

public sealed class MedicalHistory : BaseEntity
{
    public Guid PatientId { get; set; }

    public string Allergies { get; set; } = "[]";

    public string ChronicDiseases { get; set; } = "[]";

    public string CurrentMedications { get; set; } = "[]";

    public string PastSurgeries { get; set; } = "[]";

    public string FamilyHistory { get; set; } = "[]";

    public string SmokingStatus { get; set; } = "Non-Smoker";

    public string AlcoholStatus { get; set; } = "Non-User";

    public Patient Patient { get; set; } = null!;
}

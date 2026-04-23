using HealthCareMS.Domain.Common;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Domain.Patients;

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public sealed class Patient : BaseEntity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Cnic { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? BloodGroup { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? Phone { get; set; }

    public string? AlternatePhone { get; set; }

    public string? AddressStreet { get; set; }

    public string? AddressCity { get; set; }

    public string? AddressProvince { get; set; }

    public string? AddressPostalCode { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public string? EmergencyContactRelation { get; set; }

    public string? InsuranceProvider { get; set; }

    public string? InsurancePolicyNo { get; set; }

    public bool IsActive { get; set; } = true;

    public ApplicationUser User { get; set; } = null!;

    public MedicalHistory? MedicalHistory { get; set; }

    public ICollection<PatientVital> Vitals { get; set; } = new List<PatientVital>();
}

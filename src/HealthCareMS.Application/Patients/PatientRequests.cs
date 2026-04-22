namespace HealthCareMS.Application.Patients;

public sealed record RegisterPatientRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? Cnic,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? Phone,
    string? AlternatePhone,
    string? AddressStreet,
    string? AddressCity,
    string? AddressProvince,
    string? AddressPostalCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation);

public sealed record UpdatePatientProfileRequest(
    string FirstName,
    string LastName,
    string? BloodGroup,
    string? Phone,
    string? AlternatePhone,
    string? AddressStreet,
    string? AddressCity,
    string? AddressProvince,
    string? AddressPostalCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelation,
    string? InsuranceProvider,
    string? InsurancePolicyNo);

public sealed record UpdateMedicalHistoryRequest(
    string Allergies,
    string ChronicDiseases,
    string CurrentMedications,
    string PastSurgeries,
    string FamilyHistory,
    string SmokingStatus,
    string AlcoholStatus);

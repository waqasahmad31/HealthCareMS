namespace HealthCareMS.Application.Patients;

public sealed record PatientResponse(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
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
    string? EmergencyContactRelation,
    string? InsuranceProvider,
    string? InsurancePolicyNo,
    bool IsActive,
    DateTimeOffset CreatedAt,
    MedicalHistoryResponse? MedicalHistory);

public sealed record MedicalHistoryResponse(
    Guid Id,
    Guid PatientId,
    string Allergies,
    string ChronicDiseases,
    string CurrentMedications,
    string PastSurgeries,
    string FamilyHistory,
    string SmokingStatus,
    string AlcoholStatus,
    DateTimeOffset? UpdatedAt);

public sealed record PatientVitalsResponse(
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

public sealed record VitalTrendResponse(
    string Metric,
    string Unit,
    decimal? LatestValue,
    decimal? PreviousValue,
    decimal? Change,
    string Direction,
    DateTimeOffset? LatestRecordedAt);

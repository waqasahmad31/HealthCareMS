namespace HealthCareMS.Application.Consultations;

public sealed record CompleteConsultationRequest(
    string Diagnosis,
    string? Icd10Code,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    IReadOnlyList<PrescriptionItemRequest> PrescriptionItems);

public sealed record DrugAllergyCheckRequest(IReadOnlyList<PrescriptionItemRequest> PrescriptionItems);

public sealed record PrescriptionItemRequest(
    string MedicineName,
    string? GenericName,
    string? Strength,
    string? Route,
    string Dosage,
    string Frequency,
    short DurationDays,
    decimal Quantity,
    string? Instructions,
    bool IsSubstitutionAllowed);

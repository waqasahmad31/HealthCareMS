namespace HealthCareMS.Application.Consultations;

public sealed record Icd10CodeResponse(
    string Code,
    string Title,
    string Chapter);

public sealed record CompleteConsultationResponse(
    Guid AppointmentId,
    string AppointmentNumber,
    string Status,
    string Diagnosis,
    string? Icd10Code,
    string? Icd10Title,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    PrescriptionResponse? Prescription);

public sealed record DrapMedicineResponse(
    Guid Id,
    string DrapRegistrationNumber,
    string BrandName,
    string GenericName,
    string? Strength,
    string DosageForm,
    string? Manufacturer,
    string AllergenKeywords,
    bool IsBanned);

public sealed record DrugAllergyWarningResponse(
    string MedicineName,
    string? GenericName,
    string MatchedAllergy,
    string Severity,
    string Message);

public sealed record PrescriptionResponse(
    Guid Id,
    string PrescriptionNumber,
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset IssuedAt,
    DateTimeOffset ValidUntil,
    string Status,
    string VerificationCode,
    string DigitalSignature,
    IReadOnlyList<PrescriptionItemResponse> Items);

public sealed record PrescriptionVerificationResponse(
    Guid Id,
    string PrescriptionNumber,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset ValidUntil,
    string PatientName,
    string DoctorName,
    string? DoctorPmdcRegistrationNumber,
    string DigitalSignature,
    bool IsValid);

public sealed record PrescriptionPdfResponse(
    byte[] Content,
    string FileName,
    string ContentType);

public sealed record PrescriptionItemResponse(
    Guid Id,
    short SortOrder,
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

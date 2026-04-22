namespace HealthCareMS.Blazor.Models;

public sealed record Icd10CodeModel(
    string Code,
    string Title,
    string Chapter);

public sealed record CompleteConsultationModel(
    string Diagnosis,
    string? Icd10Code,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    IReadOnlyList<PrescriptionItemModel> PrescriptionItems);

public sealed record PrescriptionItemModel(
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

public sealed record CompleteConsultationResultModel(
    Guid AppointmentId,
    string AppointmentNumber,
    string Status,
    string Diagnosis,
    string? Icd10Code,
    string? Icd10Title,
    string? ClinicalNotes,
    DateOnly? FollowUpDate,
    PrescriptionResultModel? Prescription);

public sealed record PrescriptionResultModel(
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
    IReadOnlyList<PrescriptionItemResultModel> Items);

public sealed record PrescriptionItemResultModel(
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

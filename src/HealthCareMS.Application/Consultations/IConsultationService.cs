using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Consultations;

public interface IConsultationService
{
    Task<IReadOnlyList<DrapMedicineResponse>> SearchDrapMedicinesAsync(string? search, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DrugAllergyWarningResponse>>> CheckDrugAllergiesAsync(
        Guid patientId,
        DrugAllergyCheckRequest request,
        CancellationToken cancellationToken);

    Task<Result<CompleteConsultationResponse>> CompleteAsync(
        Guid appointmentId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Icd10CodeResponse>> SearchIcd10Async(string? search, CancellationToken cancellationToken);

    Task<Result<PrescriptionResponse>> GetPrescriptionByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<Result<PrescriptionVerificationResponse>> VerifyPrescriptionAsync(
        Guid prescriptionId,
        string verificationCode,
        CancellationToken cancellationToken);

    Task<Result<PrescriptionPdfResponse>> GeneratePrescriptionPdfAsync(Guid prescriptionId, CancellationToken cancellationToken);
}

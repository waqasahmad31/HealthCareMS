using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Patients;

public interface IPatientService
{
    Task<Result<PatientResponse>> RegisterAsync(RegisterPatientRequest request, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> GetByIdAsync(Guid patientId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PatientResponse>> SearchAsync(string? search, CancellationToken cancellationToken);

    Task<Result<PatientResponse>> UpdateProfileAsync(Guid patientId, UpdatePatientProfileRequest request, CancellationToken cancellationToken);

    Task<Result<MedicalHistoryResponse>> UpdateMedicalHistoryAsync(Guid patientId, UpdateMedicalHistoryRequest request, CancellationToken cancellationToken);

    Task<Result<PatientVitalsResponse>> RecordVitalsAsync(Guid patientId, RecordVitalsRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PatientVitalsResponse>>> GetVitalsHistoryAsync(
        Guid patientId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VitalTrendResponse>>> GetVitalsTrendsAsync(Guid patientId, CancellationToken cancellationToken);
}

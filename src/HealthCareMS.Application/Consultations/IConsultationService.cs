using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Consultations;

public interface IConsultationService
{
    Task<Result<CompleteConsultationResponse>> CompleteAsync(
        Guid appointmentId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Icd10CodeResponse>> SearchIcd10Async(string? search, CancellationToken cancellationToken);

    Task<Result<PrescriptionResponse>> GetPrescriptionByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
}

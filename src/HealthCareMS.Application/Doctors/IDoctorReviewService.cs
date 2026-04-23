using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Doctors;

public interface IDoctorReviewService
{
    Task<Result<DoctorReviewResponse>> SubmitReviewAsync(
        Guid appointmentId,
        SubmitDoctorReviewRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorReviewResponse>> GetDoctorReviewsAsync(
        Guid doctorId,
        CancellationToken cancellationToken);

    Task<Result<DoctorRatingSummaryResponse>> GetDoctorRatingSummaryAsync(
        Guid doctorId,
        CancellationToken cancellationToken);
}

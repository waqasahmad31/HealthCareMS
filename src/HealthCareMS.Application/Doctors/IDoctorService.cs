using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Doctors;

public interface IDoctorService
{
    Task<Result<DoctorResponse>> CreateProfileAsync(CreateDoctorProfileRequest request, CancellationToken cancellationToken);

    Task<Result<DoctorResponse>> GetByIdAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorResponse>> SearchAsync(string? specialization, string? city, decimal? maxFee, CancellationToken cancellationToken);

    Task<Result<DoctorResponse>> UpdateProfileAsync(Guid doctorId, UpdateDoctorProfileRequest request, CancellationToken cancellationToken);

    Task<Result<DoctorResponse>> VerifyAsync(Guid doctorId, VerifyDoctorRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<DoctorScheduleResponse>>> SetScheduleAsync(Guid doctorId, SetDoctorScheduleRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date, string appointmentType, CancellationToken cancellationToken);

    Task<IReadOnlyList<DoctorRecommendationResponse>> GetRecommendationsAsync(
        DoctorRecommendationRequest request,
        CancellationToken cancellationToken);
}

using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Labs;

public interface ILabService
{
    Task<IReadOnlyList<LabTestResponse>> SearchTestsAsync(string? search, CancellationToken cancellationToken);

    Task<Result<LabBookingResponse>> CreateConsultationLabOrderAsync(
        Guid appointmentId,
        CreateConsultationLabOrderRequest request,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<LabBookingResponse>>> GetBookingsByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken);
}

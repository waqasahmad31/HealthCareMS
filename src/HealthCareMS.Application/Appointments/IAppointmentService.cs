using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Appointments;

public interface IAppointmentService
{
    Task<Result<AppointmentResponse>> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> GetByIdAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AppointmentResponse>> SearchAsync(Guid? patientId, Guid? doctorId, string? status, DateOnly? date, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> ConfirmAsync(Guid appointmentId, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> CancelAsync(Guid appointmentId, CancelAppointmentRequest request, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> RescheduleAsync(Guid appointmentId, RescheduleAppointmentRequest request, CancellationToken cancellationToken);

    Task<Result<AppointmentResponse>> CompleteAsync(Guid appointmentId, CompleteAppointmentRequest request, CancellationToken cancellationToken);
}

using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Portals;

public interface IPortalService
{
    Task<Result<DoctorPortalDashboardResponse>> GetDoctorDashboardAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PortalDoctorScheduleResponse>>> GetDoctorScheduleAsync(Guid doctorId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PortalAppointmentSummaryResponse>>> GetDoctorAppointmentsAsync(Guid doctorId, DateOnly date, CancellationToken cancellationToken);

    Task<Result<PatientHistoryResponse>> GetPatientHistoryForDoctorAsync(Guid doctorId, Guid patientId, CancellationToken cancellationToken);

    Task<Result<PatientPortalDashboardResponse>> GetPatientDashboardAsync(Guid patientId, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<PortalAppointmentSummaryResponse>>> GetPatientAppointmentsAsync(Guid patientId, string? status, CancellationToken cancellationToken);

    Task<Result<PortalAppointmentDetailResponse>> GetPatientAppointmentDetailAsync(Guid patientId, Guid appointmentId, CancellationToken cancellationToken);
}

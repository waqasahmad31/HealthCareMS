using HealthCareMS.Shared.Common;

namespace HealthCareMS.Application.Admin;

public interface IAdminOperationsService
{
    Task<AdminAppointmentOverviewResponse> GetAppointmentOverviewAsync(
        Guid? patientId,
        Guid? doctorId,
        string? status,
        DateOnly? date,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    Task<AdminDoctorManagementResponse> GetDoctorManagementAsync(
        string? specialization,
        string? city,
        bool? isVerified,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<Result<AdminDoctorRowResponse>> SetDoctorStatusAsync(
        Guid doctorId,
        UpdateDoctorAdminStatusRequest request,
        CancellationToken cancellationToken);

    Task<SystemConfigurationResponse> GetSystemConfigurationAsync(CancellationToken cancellationToken);

    Task<Result<SystemSettingResponse>> UpdateSystemSettingAsync(
        string settingKey,
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken);
}

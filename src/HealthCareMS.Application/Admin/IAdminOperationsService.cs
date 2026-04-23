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

    Task<Result<NavigationMenuResponse>> GetNavigationMenuAsync(
        string? culture,
        CancellationToken cancellationToken);

    Task<Result<NavigationConfigurationResponse>> GetNavigationConfigurationAsync(CancellationToken cancellationToken);

    Task<Result<NavigationConfigurationResponse>> UpdateNavigationConfigurationAsync(
        UpdateNavigationConfigurationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationGroupResponse>> GetNavigationGroupsAsync(CancellationToken cancellationToken);

    Task<Result<NavigationGroupResponse>> CreateNavigationGroupAsync(CreateNavigationGroupRequest request, CancellationToken cancellationToken);

    Task<Result<NavigationGroupResponse>> UpdateNavigationGroupAsync(Guid groupId, UpdateNavigationGroupRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteNavigationGroupAsync(Guid groupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationItemResponse>> GetNavigationItemsTreeAsync(CancellationToken cancellationToken);

    Task<Result<NavigationItemResponse>> CreateNavigationItemAsync(CreateNavigationItemRequest request, CancellationToken cancellationToken);

    Task<Result<NavigationItemResponse>> UpdateNavigationItemAsync(Guid itemId, UpdateNavigationItemRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteNavigationItemAsync(Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<NavigationIconResponse>> GetNavigationIconsAsync(CancellationToken cancellationToken);

    Task<Result<NavigationIconResponse>> CreateNavigationIconAsync(CreateNavigationIconRequest request, CancellationToken cancellationToken);

    Task<Result<NavigationIconResponse>> UpdateNavigationIconAsync(Guid iconId, UpdateNavigationIconRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteNavigationIconAsync(Guid iconId, CancellationToken cancellationToken);
}

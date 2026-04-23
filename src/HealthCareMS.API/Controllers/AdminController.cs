using HealthCareMS.API.Security;
using HealthCareMS.Application.Admin;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/admin")]
public sealed class AdminController(IAdminOperationsService adminOperationsService) : ApiControllerBase
{
    [HttpGet("appointments/overview")]
    [RequirePermission(PermissionKeys.System.ReportsGlobal)]
    public async Task<IActionResult> GetAppointmentOverview(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetAppointmentOverviewAsync(
            patientId,
            doctorId,
            status,
            date,
            pageNumber <= 0 ? 1 : pageNumber,
            pageSize <= 0 ? 50 : pageSize,
            cancellationToken);

        return OkEnvelope(response);
    }

    [HttpGet("doctors/management")]
    [RequirePermission(PermissionKeys.Doctor.Verify)]
    public async Task<IActionResult> GetDoctorManagement(
        [FromQuery] string? specialization,
        [FromQuery] string? city,
        [FromQuery] bool? isVerified,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetDoctorManagementAsync(
            specialization,
            city,
            isVerified,
            isActive,
            cancellationToken);

        return OkEnvelope(response);
    }

    [HttpPut("doctors/{doctorId:guid}/status")]
    [RequirePermission(PermissionKeys.Doctor.Verify)]
    public async Task<IActionResult> SetDoctorStatus(
        Guid doctorId,
        UpdateDoctorAdminStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.SetDoctorStatusAsync(doctorId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("system-configuration")]
    [RequirePermission(PermissionKeys.Tenant.SettingsUpdate)]
    public async Task<IActionResult> GetSystemConfiguration(CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetSystemConfigurationAsync(cancellationToken);
        return OkEnvelope(response);
    }

    [HttpPut("system-configuration/{settingKey}")]
    [RequirePermission(PermissionKeys.Tenant.SettingsUpdate)]
    public async Task<IActionResult> UpdateSystemSetting(
        string settingKey,
        UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.UpdateSystemSettingAsync(settingKey, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("navigation/configuration")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> GetNavigationConfiguration(CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.GetNavigationConfigurationAsync(cancellationToken);
        return FromResult(result);
    }

    [HttpPut("navigation/configuration")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> UpdateNavigationConfiguration(
        UpdateNavigationConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.UpdateNavigationConfigurationAsync(request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("navigation/groups")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> GetNavigationGroups(CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetNavigationGroupsAsync(cancellationToken);
        return OkEnvelope(response);
    }

    [HttpPost("navigation/groups")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> CreateNavigationGroup(
        CreateNavigationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.CreateNavigationGroupAsync(request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("navigation/groups/{groupId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> UpdateNavigationGroup(
        Guid groupId,
        UpdateNavigationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.UpdateNavigationGroupAsync(groupId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("navigation/groups/{groupId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> DeleteNavigationGroup(Guid groupId, CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.DeleteNavigationGroupAsync(groupId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("navigation/items")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> GetNavigationItems(CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetNavigationItemsTreeAsync(cancellationToken);
        return OkEnvelope(response);
    }

    [HttpPost("navigation/items")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> CreateNavigationItem(
        CreateNavigationItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.CreateNavigationItemAsync(request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("navigation/items/{itemId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> UpdateNavigationItem(
        Guid itemId,
        UpdateNavigationItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.UpdateNavigationItemAsync(itemId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("navigation/items/{itemId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> DeleteNavigationItem(Guid itemId, CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.DeleteNavigationItemAsync(itemId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("navigation/icons")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> GetNavigationIcons(CancellationToken cancellationToken)
    {
        var response = await adminOperationsService.GetNavigationIconsAsync(cancellationToken);
        return OkEnvelope(response);
    }

    [HttpPost("navigation/icons")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> CreateNavigationIcon(
        CreateNavigationIconRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.CreateNavigationIconAsync(request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("navigation/icons/{iconId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> UpdateNavigationIcon(
        Guid iconId,
        UpdateNavigationIconRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.UpdateNavigationIconAsync(iconId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("navigation/icons/{iconId:guid}")]
    [RequirePermission(PermissionKeys.System.UsersViewAll)]
    public async Task<IActionResult> DeleteNavigationIcon(Guid iconId, CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.DeleteNavigationIconAsync(iconId, cancellationToken);
        return FromResult(result);
    }
}

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
}

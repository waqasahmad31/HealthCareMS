using HealthCareMS.API.Security;
using HealthCareMS.Application.Portals;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/portal")]
public sealed class PortalController(IPortalService portalService) : ApiControllerBase
{
    [HttpGet("doctors/{doctorId:guid}/dashboard")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetDoctorDashboard(
        Guid doctorId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var dashboardDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await portalService.GetDoctorDashboardAsync(doctorId, dashboardDate, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("doctors/{doctorId:guid}/schedule")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetDoctorSchedule(Guid doctorId, CancellationToken cancellationToken)
    {
        var result = await portalService.GetDoctorScheduleAsync(doctorId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("doctors/{doctorId:guid}/appointments")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetDoctorAppointments(
        Guid doctorId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var appointmentDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await portalService.GetDoctorAppointmentsAsync(doctorId, appointmentDate, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("doctors/{doctorId:guid}/patients/{patientId:guid}/history")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOthers)]
    public async Task<IActionResult> GetPatientHistoryForDoctor(
        Guid doctorId,
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var result = await portalService.GetPatientHistoryForDoctorAsync(doctorId, patientId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("patients/{patientId:guid}/dashboard")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOwn)]
    public async Task<IActionResult> GetPatientDashboard(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await portalService.GetPatientDashboardAsync(patientId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("patients/{patientId:guid}/appointments")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOwn)]
    public async Task<IActionResult> GetPatientAppointments(
        Guid patientId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var result = await portalService.GetPatientAppointmentsAsync(patientId, status, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("patients/{patientId:guid}/appointments/{appointmentId:guid}")]
    [RequirePermission(PermissionKeys.Patient.RecordsViewOwn)]
    public async Task<IActionResult> GetPatientAppointmentDetail(
        Guid patientId,
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var result = await portalService.GetPatientAppointmentDetailAsync(patientId, appointmentId, cancellationToken);
        return FromResult(result);
    }
}

using HealthCareMS.API.Security;
using HealthCareMS.Application.Appointments;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/appointments")]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionKeys.Appointment.Book)]
    public async Task<IActionResult> Book(BookAppointmentRequest request, CancellationToken cancellationToken)
    {
        var result = await appointmentService.BookAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await appointmentService.GetByIdAsync(id, cancellationToken);
        return FromResult(result);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> Search(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var appointments = await appointmentService.SearchAsync(patientId, doctorId, status, date, cancellationToken);
        return OkEnvelope(appointments);
    }

    [HttpPut("{id:guid}/confirm")]
    [RequirePermission(PermissionKeys.Appointment.Confirm)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        var result = await appointmentService.ConfirmAsync(id, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Appointment.Cancel)]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancelAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.CancelAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/reschedule")]
    [RequirePermission(PermissionKeys.Appointment.Book)]
    public async Task<IActionResult> Reschedule(
        Guid id,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.RescheduleAsync(id, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.Appointment.Complete)]
    public async Task<IActionResult> Complete(
        Guid id,
        CompleteAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await appointmentService.CompleteAsync(id, request, cancellationToken);
        return FromResult(result);
    }
}

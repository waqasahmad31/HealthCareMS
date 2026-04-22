using HealthCareMS.API.Hubs;
using HealthCareMS.API.Security;
using HealthCareMS.Application.Queues;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/queue")]
public sealed class QueueController(
    IQueueService queueService,
    IHubContext<QueueHub> queueHub) : ApiControllerBase
{
    [HttpPost("walk-ins")]
    [RequirePermission(PermissionKeys.Appointment.Book)]
    public async Task<IActionResult> RegisterWalkIn(
        WalkInRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await queueService.RegisterWalkInAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            await BroadcastQueueChangesAsync(result.Value, cancellationToken);
        }

        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpPut("{appointmentId:guid}/check-in")]
    [RequirePermission(PermissionKeys.Appointment.Confirm)]
    public async Task<IActionResult> CheckIn(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await queueService.CheckInAsync(appointmentId, cancellationToken);
        if (result.IsSuccess)
        {
            await BroadcastQueueChangesAsync(result.Value, cancellationToken);
        }

        return FromResult(result);
    }

    [HttpPost("doctors/{doctorId:guid}/call-next")]
    [RequirePermission(PermissionKeys.Appointment.Confirm)]
    public async Task<IActionResult> CallNext(
        Guid doctorId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var queueDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await queueService.CallNextAsync(doctorId, queueDate, cancellationToken);
        if (result.IsSuccess)
        {
            await BroadcastQueueChangesAsync(result.Value, cancellationToken);
        }

        return FromResult(result);
    }

    [HttpGet("doctors/{doctorId:guid}/next")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetNextPatient(
        Guid doctorId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var queueDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await queueService.GetNextPatientAsync(doctorId, queueDate, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("doctors/{doctorId:guid}")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetBoard(
        Guid doctorId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var queueDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await queueService.GetBoardAsync(doctorId, queueDate, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("patients/{appointmentId:guid}")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetPatientStatus(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await queueService.GetPatientStatusAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    private async Task BroadcastQueueChangesAsync(QueueEntryResponse entry, CancellationToken cancellationToken)
    {
        var date = DateOnly.FromDateTime(entry.ScheduledAt.UtcDateTime);
        var boardResult = await queueService.GetBoardAsync(entry.DoctorId, date, cancellationToken);
        if (boardResult.IsSuccess)
        {
            await queueHub.Clients
                .Group(QueueHub.QueueGroup(entry.DoctorId, date))
                .SendAsync("QueueUpdated", boardResult.Value, cancellationToken);
        }

        var patientStatus = await queueService.GetPatientStatusAsync(entry.AppointmentId, cancellationToken);
        if (patientStatus.IsSuccess)
        {
            await queueHub.Clients
                .Group(QueueHub.PatientGroup(entry.AppointmentId))
                .SendAsync("PatientQueueUpdated", patientStatus.Value, cancellationToken);
        }
    }
}

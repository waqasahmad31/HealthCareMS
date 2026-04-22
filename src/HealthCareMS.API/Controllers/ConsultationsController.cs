using HealthCareMS.API.Security;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/consultations")]
public sealed class ConsultationsController(
    IConsultationService consultationService,
    IConsultationSessionService consultationSessionService) : ApiControllerBase
{
    [HttpPost("sessions")]
    [RequirePermission(PermissionKeys.Consultation.VideoStart)]
    public async Task<IActionResult> StartSession(
        StartConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.StartAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [Authorize]
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.GetByIdAsync(sessionId, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpGet("appointments/{appointmentId:guid}/session")]
    public async Task<IActionResult> GetSessionByAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.GetByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("sessions/{sessionId:guid}/join")]
    public async Task<IActionResult> JoinSession(
        Guid sessionId,
        JoinConsultationSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationSessionService.JoinAsync(sessionId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("appointments/{appointmentId:guid}/complete")]
    [RequirePermission(PermissionKeys.Consultation.PrescriptionCreate)]
    public async Task<IActionResult> CompleteAppointment(
        Guid appointmentId,
        CompleteConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await consultationService.CompleteAsync(appointmentId, request, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("appointments/{appointmentId:guid}/prescription")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> GetPrescription(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await consultationService.GetPrescriptionByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("icd10")]
    [RequirePermission(PermissionKeys.Appointment.View)]
    public async Task<IActionResult> SearchIcd10([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await consultationService.SearchIcd10Async(search, cancellationToken);
        return OkEnvelope(results);
    }
}

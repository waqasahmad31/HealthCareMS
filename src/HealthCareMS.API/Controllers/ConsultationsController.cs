using HealthCareMS.API.Security;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/consultations")]
public sealed class ConsultationsController(IConsultationService consultationService) : ApiControllerBase
{
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

using HealthCareMS.API.Security;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/lab")]
public sealed class LabController(ILabService labService) : ApiControllerBase
{
    [HttpGet("tests")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> SearchTests([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var results = await labService.SearchTestsAsync(search, cancellationToken);
        return OkEnvelope(results);
    }

    [HttpGet("appointments/{appointmentId:guid}/bookings")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> GetBookingsByAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await labService.GetBookingsByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }
}

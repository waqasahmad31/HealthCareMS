using HealthCareMS.API.Security;
using HealthCareMS.Application.Labs;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Shared.Common;
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

    [HttpPost("tests/import")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> ImportTests(
        [FromForm] IFormFile file,
        [FromForm] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Fail(new Error("LAB_TEST_CSV_REQUIRED", "CSV file is required."));
        }

        using var reader = new StreamReader(file.OpenReadStream());
        var csv = await reader.ReadToEndAsync(cancellationToken);
        var result = await labService.ImportTestsCsvAsync(new ImportLabTestsCsvRequest(tenantId, csv), cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("panels")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> GetPanels([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var result = await labService.GetPanelsAsync(search, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("panels")]
    [RequirePermission(PermissionKeys.Lab.ResultsEntry)]
    public async Task<IActionResult> CreatePanel(CreateLabPanelRequest request, CancellationToken cancellationToken)
    {
        var result = await labService.CreatePanelAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }

    [HttpGet("appointments/{appointmentId:guid}/bookings")]
    [RequirePermission(PermissionKeys.Lab.TestsView)]
    public async Task<IActionResult> GetBookingsByAppointment(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await labService.GetBookingsByAppointmentAsync(appointmentId, cancellationToken);
        return FromResult(result);
    }
}

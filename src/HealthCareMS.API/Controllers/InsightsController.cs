using HealthCareMS.API.Security;
using HealthCareMS.Application.Insights;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/insights")]
public sealed class InsightsController(IInsightService insightService) : ApiControllerBase
{
    [HttpGet("patients/{patientId:guid}/timeline")]
    [Authorize]
    public async Task<IActionResult> GetPatientTimeline(
        Guid patientId,
        [FromQuery] string? filter,
        CancellationToken cancellationToken)
    {
        var result = await insightService.GetPatientTimelineAsync(patientId, filter, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("patients/{patientId:guid}/health-summary.pdf")]
    [Authorize]
    public async Task<IActionResult> DownloadHealthSummary(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await insightService.GenerateHealthSummaryPdfAsync(patientId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("patients/{patientId:guid}/emergency-card.pdf")]
    [Authorize]
    public async Task<IActionResult> DownloadEmergencyCard(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await insightService.GenerateEmergencyCardPdfAsync(patientId, cancellationToken);
        if (result.IsFailure)
        {
            return FromResult(result);
        }

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("analytics")]
    [RequirePermission(PermissionKeys.System.ReportsGlobal)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] Guid? tenantId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var result = await insightService.GetAnalyticsAsync(tenantId, from, to, cancellationToken);
        return FromResult(result);
    }
}

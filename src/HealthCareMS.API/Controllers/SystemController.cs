using HealthCareMS.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/system")]
public sealed class SystemController(IAdminOperationsService adminOperationsService) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("ping")]
    [OutputCache(PolicyName = "PublicGetShort")]
    public IActionResult Ping()
    {
        return OkEnvelope(new
        {
            Service = "HealthCareMS.API",
            Status = "Online",
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    [Authorize]
    [HttpGet("navigation")]
    public async Task<IActionResult> GetNavigationMenu(
        [FromQuery] string? culture,
        CancellationToken cancellationToken)
    {
        var result = await adminOperationsService.GetNavigationMenuAsync(culture, cancellationToken);
        return FromResult(result);
    }
}

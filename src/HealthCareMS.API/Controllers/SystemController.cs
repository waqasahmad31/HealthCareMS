using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/system")]
public sealed class SystemController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return OkEnvelope(new
        {
            Service = "HealthCareMS.API",
            Status = "Online",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}

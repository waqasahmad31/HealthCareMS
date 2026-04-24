using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/security")]
[Authorize]
public sealed class SecurityController(ISecurityCenterService securityCenterService, ICurrentUser currentUser) : ApiControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.GetOverviewAsync(currentUser.UserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("active-sessions")]
    public async Task<IActionResult> GetActiveSessions(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.GetActiveSessionsAsync(currentUser.UserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpGet("login-history")]
    public async Task<IActionResult> GetLoginHistory(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.GetLoginHistoryAsync(currentUser.UserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("active-sessions/{sessionId:guid}/revoke")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.RevokeSessionAsync(currentUser.UserId.Value, sessionId, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("2fa/setup")]
    public async Task<IActionResult> BeginTwoFactorSetup(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.BeginTwoFactorSetupAsync(currentUser.UserId.Value, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("2fa/enable")]
    public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.EnableTwoFactorAsync(currentUser.UserId.Value, request, cancellationToken);
        return FromResult(result);
    }

    [HttpPost("2fa/disable")]
    public async Task<IActionResult> DisableTwoFactor(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var result = await securityCenterService.DisableTwoFactorAsync(currentUser.UserId.Value, cancellationToken);
        return FromResult(result);
    }
}

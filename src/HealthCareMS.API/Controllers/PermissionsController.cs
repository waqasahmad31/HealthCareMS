using HealthCareMS.API.Security;
using HealthCareMS.Application.Identity;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/permissions")]
public sealed class PermissionsController(IIdentityManagementService identityManagementService) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.Tenant.PermissionsGrant)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var permissions = await identityManagementService.GetPermissionsAsync(cancellationToken);
        return OkEnvelope(permissions);
    }

    [HttpPost("grant")]
    [RequirePermission(PermissionKeys.Tenant.PermissionsGrant)]
    public async Task<IActionResult> Grant(GrantPermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await identityManagementService.GrantPermissionsAsync(request, cancellationToken);
        return FromResult(result);
    }
}

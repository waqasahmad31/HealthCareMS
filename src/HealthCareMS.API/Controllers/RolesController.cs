using HealthCareMS.API.Security;
using HealthCareMS.Application.Identity;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/roles")]
public sealed class RolesController(IIdentityManagementService identityManagementService) : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionKeys.Tenant.RolesCreate)]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await identityManagementService.CreateRoleAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }
}

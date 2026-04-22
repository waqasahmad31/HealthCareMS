using HealthCareMS.API.Security;
using HealthCareMS.Application.Tenants;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/tenants")]
public sealed class TenantsController(ITenantService tenantService) : ApiControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionKeys.System.SuperAdminAll)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await tenantService.GetAllAsync(cancellationToken);
        return OkEnvelope(tenants);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.System.TenantsCreate)]
    public async Task<IActionResult> Create(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var result = await tenantService.CreateAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }
}

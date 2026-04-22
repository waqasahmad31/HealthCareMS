using HealthCareMS.API.Security;
using HealthCareMS.Application.Identity;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HealthCareMS.API.Controllers;

[Route("api/v1/users")]
public sealed class UsersController(IIdentityManagementService identityManagementService) : ApiControllerBase
{
    [HttpPost]
    [RequirePermission(PermissionKeys.Tenant.UsersCreate)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await identityManagementService.CreateUserAsync(request, cancellationToken);
        return FromResult(result, StatusCodes.Status201Created);
    }
}

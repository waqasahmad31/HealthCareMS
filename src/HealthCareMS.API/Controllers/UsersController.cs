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

    [HttpGet("manageable")]
    [RequirePermission(PermissionKeys.Tenant.UsersAssignRole)]
    public async Task<IActionResult> GetManageableUsers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var users = await identityManagementService.GetManageableUsersAsync(search, cancellationToken);
        return OkEnvelope(users);
    }

    [HttpGet("{userId:guid}/menu-assignment")]
    [RequirePermission(PermissionKeys.Tenant.UsersAssignRole)]
    public async Task<IActionResult> GetMenuAssignment(Guid userId, CancellationToken cancellationToken)
    {
        var result = await identityManagementService.GetUserMenuAssignmentAsync(userId, cancellationToken);
        return FromResult(result);
    }

    [HttpPut("{userId:guid}/menu-assignment")]
    [RequirePermission(PermissionKeys.Tenant.UsersAssignRole)]
    public async Task<IActionResult> AssignMenu(
        Guid userId,
        AssignUserMenuRequest request,
        CancellationToken cancellationToken)
    {
        var result = await identityManagementService.AssignUserMenuAsync(userId, request, cancellationToken);
        return FromResult(result);
    }
}

using System.Security.Claims;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.API.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
        out var userId)
        ? userId
        : null;

    public Guid? TenantId => Guid.TryParse(
        Principal?.FindFirstValue("TenantId"),
        out var tenantId)
        ? tenantId
        : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin => Permissions.Contains(PermissionKeys.System.SuperAdminAll, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("Permission").Select(x => x.Value).ToArray() ?? [];
}

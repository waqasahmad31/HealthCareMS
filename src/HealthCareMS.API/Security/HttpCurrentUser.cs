using System.Security.Claims;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Identity;

namespace HealthCareMS.API.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Principal?.FindFirstValue("sub"),
        out var userId)
        ? userId
        : null;

    public Guid? TenantId => Guid.TryParse(
        Principal?.FindFirstValue("TenantId")
        ?? Principal?.FindFirstValue("tenant_id"),
        out var tenantId)
        ? tenantId
        : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin => Permissions.Contains(PermissionKeys.System.SuperAdminAll, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            if (Principal is null)
            {
                return [];
            }

            return Principal.Claims
                .Where(claim =>
                    string.Equals(claim.Type, "Permission", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(claim.Type, "scope", StringComparison.OrdinalIgnoreCase))
                .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}

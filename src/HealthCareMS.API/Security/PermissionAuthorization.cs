using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace HealthCareMS.API.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permissionKey)
    {
        PermissionKey = permissionKey;
        Policy = PolicyPrefix + permissionKey;
    }

    public string PermissionKey { get; }
}

public sealed record PermissionRequirement(string PermissionKey) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissions = context.User.FindAll("Permission").Select(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissions.Contains(PermissionKeys.System.SuperAdminAll) || permissions.Contains(requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

public sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionKey = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionKey))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

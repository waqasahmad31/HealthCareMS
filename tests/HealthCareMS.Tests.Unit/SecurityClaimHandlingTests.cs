using System.Security.Claims;
using HealthCareMS.API.Security;
using HealthCareMS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace HealthCareMS.Tests.Unit;

public sealed class SecurityClaimHandlingTests
{
    [Fact]
    public void HttpCurrentUser_ShouldResolveSubjectTenantAndPermissionsAcrossClaimFormats()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", Guid.Parse("37e0b3f4-4a29-4b9a-b5a7-e3536dc00e8e").ToString()),
                new Claim("tenant_id", Guid.Parse("731f445f-d1bc-437a-8f1e-f2dc8d3dcc04").ToString()),
                new Claim("Permission", PermissionKeys.Appointment.View),
                new Claim("scope", $"{PermissionKeys.Doctor.Verify} {PermissionKeys.System.SuperAdminAll}")
            ], "Bearer"))
        };

        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new HttpCurrentUser(accessor);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(Guid.Parse("37e0b3f4-4a29-4b9a-b5a7-e3536dc00e8e"), currentUser.UserId);
        Assert.Equal(Guid.Parse("731f445f-d1bc-437a-8f1e-f2dc8d3dcc04"), currentUser.TenantId);
        Assert.Contains(PermissionKeys.Appointment.View, currentUser.Permissions);
        Assert.Contains(PermissionKeys.Doctor.Verify, currentUser.Permissions);
        Assert.True(currentUser.IsSuperAdmin);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_ShouldAcceptPermissionFromScopeClaim()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("scope", PermissionKeys.Patient.RecordsViewOwn)
        ], "Bearer");
        var user = new ClaimsPrincipal(identity);
        var requirement = new PermissionRequirement(PermissionKeys.Patient.RecordsViewOwn);
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}

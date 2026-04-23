using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Auth;
using HealthCareMS.Application.Identity;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Identity;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Integration;

public sealed class AuthAndIdentityManagementTests
{
    [Fact]
    public async Task RefreshTokenAsync_ShouldRotateToken_AndLogoutShouldClearIt()
    {
        await using var dbContext = CreateDbContext();
        var hasher = new Pbkdf2PasswordHasher();
        var role = new Role { Name = "SuperAdmin", IsSystemRole = true };
        var permission = NewPermission(PermissionKeys.System.SuperAdminAll);
        role.RolePermissions.Add(new RolePermission { Role = role, Permission = permission, PermissionId = permission.Id });
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);

        var user = new ApplicationUser
        {
            Role = role,
            RoleId = role.Id,
            FirstName = "Super",
            LastName = "Admin",
            Email = "superadmin@example.com",
            PasswordHash = hasher.Hash("StrongPass123"),
            IsActive = true,
            IsEmailVerified = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var authService = new AuthService(dbContext, hasher, CreateJwtService());
        var login = await authService.LoginAsync(new LoginRequest("superadmin@example.com", "StrongPass123"), CancellationToken.None);
        var refresh = await authService.RefreshTokenAsync(new RefreshTokenRequest(login.Value.RefreshToken), CancellationToken.None);
        var logout = await authService.LogoutAsync(new LogoutRequest(user.Id), CancellationToken.None);

        Assert.True(login.IsSuccess);
        Assert.True(refresh.IsSuccess);
        Assert.NotEqual(login.Value.RefreshToken, refresh.Value.RefreshToken);
        Assert.True(logout.IsSuccess);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiry);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldRejectPermissionsAboveCurrentUserCeiling()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Name = "Care Pharmacy",
            TenantType = TenantType.Pharmacy,
            OwnerName = "Owner",
            Phone = "+923001234567",
            Email = "owner@example.com",
            Address = "{}"
        };

        var allowedPermission = NewPermission(PermissionKeys.Tenant.UsersCreate);
        var forbiddenPermission = NewPermission(PermissionKeys.Pharmacy.OrdersProcess);

        dbContext.Tenants.Add(tenant);
        dbContext.Permissions.AddRange(allowedPermission, forbiddenPermission);
        await dbContext.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(
            Guid.NewGuid(),
            tenant.Id,
            false,
            [PermissionKeys.Tenant.UsersCreate]);

        var service = new IdentityManagementService(dbContext, currentUser, new Pbkdf2PasswordHasher());
        var result = await service.CreateRoleAsync(
            new CreateRoleRequest(
                tenant.Id,
                "Limited Manager",
                [PermissionKeys.Tenant.UsersCreate, PermissionKeys.Pharmacy.OrdersProcess]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("PERMISSION_CEILING_EXCEEDED", result.Error.Code);
    }

    [Fact]
    public async Task CreateRoleAsync_ShouldUseCurrentTenant_WhenTenantAdminOmitsTenantId()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Name = "Care Lab",
            TenantType = TenantType.Lab,
            OwnerName = "Owner",
            Phone = "+923001234567",
            Email = "lab@example.com",
            Address = "{}"
        };

        var permission = NewPermission(PermissionKeys.Tenant.UsersCreate);
        dbContext.Tenants.Add(tenant);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(
            Guid.NewGuid(),
            tenant.Id,
            false,
            [PermissionKeys.Tenant.UsersCreate]);

        var service = new IdentityManagementService(dbContext, currentUser, new Pbkdf2PasswordHasher());
        var result = await service.CreateRoleAsync(
            new CreateRoleRequest(null, "Tenant Clerk", [PermissionKeys.Tenant.UsersCreate]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(tenant.Id, result.Value.TenantId);
        Assert.Contains(PermissionKeys.Tenant.UsersCreate, result.Value.Permissions);
    }

    [Fact]
    public async Task AssignUserMenuAsync_ShouldPersistExpandedAssignmentsInDedicatedTable()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Name = "Care Ops",
            TenantType = TenantType.Hospital,
            OwnerName = "Owner",
            Phone = "+923001234567",
            Email = "ops@example.com",
            Address = "{}"
        };
        var role = new Role { Name = "OperationsLead", Tenant = tenant, TenantId = tenant.Id };
        var currentUserEntity = new ApplicationUser
        {
            Tenant = tenant,
            TenantId = tenant.Id,
            Role = role,
            RoleId = role.Id,
            FirstName = "Areeb",
            LastName = "Lead",
            Email = "lead@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var targetUser = new ApplicationUser
        {
            Tenant = tenant,
            TenantId = tenant.Id,
            Role = role,
            RoleId = role.Id,
            FirstName = "Sara",
            LastName = "Agent",
            Email = "agent@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true,
            CreatedByUserId = currentUserEntity.Id
        };
        var group = new NavigationGroup
        {
            TenantId = tenant.Id,
            Key = "operations",
            LabelEn = "Operations",
            LabelUr = "آپریشنز",
            SortOrder = 10,
            IsActive = true
        };
        var parent = new NavigationItem
        {
            NavigationGroup = group,
            NavigationGroupId = group.Id,
            Key = "parent-menu",
            LabelEn = "Parent Menu",
            LabelUr = "پیرنٹ مینو",
            Icon = "dashboard",
            Route = "parent",
            SortOrder = 10,
            RequiredPermissionsJson = "[\"tenant.users.create\"]",
            IsActive = true
        };
        var child = new NavigationItem
        {
            NavigationGroup = group,
            NavigationGroupId = group.Id,
            ParentItem = parent,
            ParentItemId = parent.Id,
            Key = "child-menu",
            LabelEn = "Child Menu",
            LabelUr = "چائلڈ مینو",
            Icon = "notifications",
            Route = "parent/child",
            SortOrder = 20,
            RequiredPermissionsJson = "[\"patient.records.view_own\"]",
            IsActive = true
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Roles.Add(role);
        dbContext.Users.AddRange(currentUserEntity, targetUser);
        dbContext.NavigationGroups.Add(group);
        dbContext.NavigationItems.AddRange(parent, child);
        await dbContext.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(
            currentUserEntity.Id,
            tenant.Id,
            false,
            [PermissionKeys.Tenant.UsersCreate, PermissionKeys.Patient.RecordsViewOwn]);
        var service = new IdentityManagementService(dbContext, currentUser, new Pbkdf2PasswordHasher());

        var result = await service.AssignUserMenuAsync(targetUser.Id, new AssignUserMenuRequest(["child-menu"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("parent-menu", result.Value.MenuItemKeys);
        Assert.Contains("child-menu", result.Value.MenuItemKeys);
        Assert.Equal(2, await dbContext.UserNavigationAssignments.CountAsync());
        Assert.False(await dbContext.SystemSettings.AnyAsync(x => x.SettingKey.StartsWith("Navigation.Assignment.User.")));
    }

    [Fact]
    public async Task GetManageableUsersAsync_ShouldReturnOnlyDirectSubUsers()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Name = "Care Ops",
            TenantType = TenantType.Hospital,
            OwnerName = "Owner",
            Phone = "+923001234567",
            Email = "ops.manage@example.com",
            Address = "{}"
        };
        var role = new Role { Name = "OperationsLead", Tenant = tenant, TenantId = tenant.Id };
        var manager = new ApplicationUser
        {
            Tenant = tenant,
            TenantId = tenant.Id,
            Role = role,
            RoleId = role.Id,
            FirstName = "Main",
            LastName = "Manager",
            Email = "manager@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true
        };
        var directUser = new ApplicationUser
        {
            Tenant = tenant,
            TenantId = tenant.Id,
            Role = role,
            RoleId = role.Id,
            FirstName = "Direct",
            LastName = "Staff",
            Email = "direct.staff@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true,
            CreatedByUserId = manager.Id
        };
        var otherUser = new ApplicationUser
        {
            Tenant = tenant,
            TenantId = tenant.Id,
            Role = role,
            RoleId = role.Id,
            FirstName = "Other",
            LastName = "Staff",
            Email = "other.staff@example.com",
            PasswordHash = "HASH",
            IsActive = true,
            IsEmailVerified = true,
            CreatedByUserId = Guid.NewGuid()
        };

        dbContext.Tenants.Add(tenant);
        dbContext.Roles.Add(role);
        dbContext.Users.AddRange(manager, directUser, otherUser);
        await dbContext.SaveChangesAsync();

        var currentUser = new FakeCurrentUser(manager.Id, tenant.Id, false, [PermissionKeys.Tenant.UsersAssignRole]);
        var service = new IdentityManagementService(dbContext, currentUser, new Pbkdf2PasswordHasher());

        var users = await service.GetManageableUsersAsync("staff", CancellationToken.None);

        var single = Assert.Single(users);
        Assert.Equal(directUser.Id, single.Id);
        Assert.Equal("Direct Staff", single.FullName);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private static JwtTokenService CreateJwtService()
    {
        return new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "HealthCareMS.Tests",
            Audience = "HealthCareMS.Tests.Client",
            SigningKey = "TEST_SIGNING_KEY_WITH_MORE_THAN_32_CHARS",
            AccessTokenMinutes = 60
        }));
    }

    private static Permission NewPermission(string permissionKey)
    {
        var parts = permissionKey.Split('.');
        return new Permission
        {
            PermissionKey = permissionKey,
            Module = parts[0],
            Action = parts[^1],
            Description = permissionKey
        };
    }

    private sealed class FakeCurrentUser(
        Guid? userId,
        Guid? tenantId,
        bool isSuperAdmin,
        IReadOnlyCollection<string> permissions) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId { get; } = tenantId;

        public bool IsAuthenticated => UserId.HasValue;

        public bool IsSuperAdmin { get; } = isSuperAdmin;

        public IReadOnlyCollection<string> Permissions { get; } = permissions;
    }
}

using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthCareMS.Infrastructure.Seed;

public sealed class DatabaseSeeder(
    HealthCareDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAsync(cancellationToken);
        await SeedSuperAdminAsync(cancellationToken);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await dbContext.Permissions
            .Select(x => x.PermissionKey)
            .ToListAsync(cancellationToken);

        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var permissionKey in PermissionKeys.All.Where(x => !existing.Contains(x)))
        {
            dbContext.Permissions.Add(new Permission
            {
                PermissionKey = permissionKey,
                Module = ToModule(permissionKey),
                Action = ToAction(permissionKey),
                Description = permissionKey
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        var superAdminRole = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.TenantId == null && x.Name == "SuperAdmin", cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = new Role { Name = "SuperAdmin", IsSystemRole = true };
            dbContext.Roles.Add(superAdminRole);
        }

        var platformRoleNames = new[] { "Patient", "Doctor" };
        foreach (var roleName in platformRoleNames)
        {
            var exists = await dbContext.Roles.AnyAsync(x => x.TenantId == null && x.Name == roleName, cancellationToken);
            if (!exists)
            {
                dbContext.Roles.Add(new Role { Name = roleName, IsSystemRole = true });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var allPermissions = await dbContext.Permissions.ToListAsync(cancellationToken);
        var currentPermissionIds = superAdminRole.RolePermissions.Select(x => x.PermissionId).ToHashSet();
        foreach (var permission in allPermissions.Where(x => !currentPermissionIds.Contains(x.Id)))
        {
            superAdminRole.RolePermissions.Add(new RolePermission
            {
                RoleId = superAdminRole.Id,
                PermissionId = permission.Id
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSuperAdminAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SuperAdmin:Email"]?.Trim().ToLowerInvariant() ?? "superadmin@healthcarems.local";
        var password = configuration["SuperAdmin:Password"] ?? "ChangeMe@12345";

        var exists = await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);
        if (exists)
        {
            return;
        }

        var role = await dbContext.Roles.SingleAsync(x => x.TenantId == null && x.Name == "SuperAdmin", cancellationToken);

        dbContext.Users.Add(new ApplicationUser
        {
            RoleId = role.Id,
            FirstName = "Super",
            LastName = "Admin",
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            IsActive = true,
            IsEmailVerified = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded Super Admin account {Email}.", email);
    }

    private static string ToModule(string permissionKey)
    {
        var first = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "System";
        return char.ToUpperInvariant(first[0]) + first[1..];
    }

    private static string ToAction(string permissionKey)
    {
        var last = permissionKey.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "*";
        return last.Length == 0 ? "*" : char.ToUpperInvariant(last[0]) + last[1..];
    }
}

public static class DatabaseSeederExtensions
{
    public static async Task SeedHealthCareDatabaseAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HealthCareDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }
}

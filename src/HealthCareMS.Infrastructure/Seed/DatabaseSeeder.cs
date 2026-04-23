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
        await SeedSystemSettingsAsync(cancellationToken);
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

        var platformRoleNames = new[] { "Patient", "Doctor", "LabAdmin", "LabStaff", "PharmacyAdmin", "Pharmacist", "DeliveryAgent" };
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
        await EnsureRolePermissionsAsync(
            "LabAdmin",
            PermissionKeys.Lab.All,
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "LabStaff",
            [
                PermissionKeys.Lab.TestsView,
                PermissionKeys.Lab.BookingCreate,
                PermissionKeys.Lab.SampleCollect,
                PermissionKeys.Lab.ResultsEntry,
                PermissionKeys.Lab.ResultsValidate,
                PermissionKeys.Lab.ResultsRelease,
                PermissionKeys.Lab.ReportsDownload
            ],
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "PharmacyAdmin",
            PermissionKeys.Pharmacy.All,
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "Pharmacist",
            [
                PermissionKeys.Pharmacy.MedicinesView,
                PermissionKeys.Pharmacy.StockView,
                PermissionKeys.Pharmacy.OrdersView,
                PermissionKeys.Pharmacy.OrdersProcess,
                PermissionKeys.Pharmacy.Dispense
            ],
            allPermissions,
            cancellationToken);
        await EnsureRolePermissionsAsync(
            "DeliveryAgent",
            [
                PermissionKeys.Pharmacy.OrdersView,
                PermissionKeys.Pharmacy.OrdersProcess
            ],
            allPermissions,
            cancellationToken);
    }

    private async Task EnsureRolePermissionsAsync(
        string roleName,
        IReadOnlyCollection<string> permissionKeys,
        IReadOnlyList<Permission> allPermissions,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .Include(x => x.RolePermissions)
            .SingleAsync(x => x.TenantId == null && x.Name == roleName, cancellationToken);
        var permissionIds = allPermissions
            .Where(x => permissionKeys.Contains(x.PermissionKey, StringComparer.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToHashSet();
        var existing = role.RolePermissions.Select(x => x.PermissionId).ToHashSet();

        foreach (var permissionId in permissionIds.Where(x => !existing.Contains(x)))
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId
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

    private async Task SeedSystemSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
            Setting("Platform.TimeZone", "Platform", "Time zone", "Asia/Karachi", "String", "Default platform time zone."),
            Setting("Platform.DefaultCurrency", "Platform", "Default currency", "PKR", "String", "Currency used for invoices and reports."),
            Setting("Platform.SupportEmail", "Platform", "Support email", "support@healthcarems.local", "String", "Public support email address."),
            Setting("Security.MaintenanceMode", "Security", "Maintenance mode", "false", "Boolean", "Temporarily limit platform access."),
            Setting("Notifications.EmailEnabled", "Notifications", "Email enabled", "true", "Boolean", "Allow outgoing email notifications."),
            Setting("Notifications.SmsEnabled", "Notifications", "SMS enabled", "false", "Boolean", "Allow outgoing SMS notifications."),
            Setting("Appointments.Reminder24HrEnabled", "Appointments", "24hr reminders", "true", "Boolean", "Schedule appointment reminders 24 hours before visit."),
            Setting("Appointments.Reminder2HrEnabled", "Appointments", "2hr reminders", "true", "Boolean", "Schedule appointment reminders 2 hours before visit."),
            Setting("Performance.SlowQueryThresholdMs", "Performance", "Slow query threshold", "500", "Number", "Threshold used by admins during performance reviews.")
        };

        var existingKeys = await dbContext.SystemSettings
            .Select(x => x.SettingKey)
            .ToListAsync(cancellationToken);
        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var setting in defaults.Where(x => !existing.Contains(x.SettingKey)))
        {
            dbContext.SystemSettings.Add(setting);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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

    private static SystemSetting Setting(
        string settingKey,
        string groupName,
        string displayName,
        string value,
        string valueType,
        string description)
    {
        return new SystemSetting
        {
            SettingKey = settingKey,
            GroupName = groupName,
            DisplayName = displayName,
            Value = value,
            ValueType = valueType,
            Description = description,
            IsEditable = true
        };
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

using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Domain.Identity;
using HealthCareMS.Infrastructure.Admin;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Configuration;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace HealthCareMS.Tests.Integration;

public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_ShouldBackfillEnterpriseNavigationWithoutRemovingExistingItems()
    {
        await using var dbContext = CreateDbContext();

        var generalGroup = new NavigationGroup
        {
            Key = "general",
            LabelEn = "General",
            LabelUr = "General",
            SortOrder = 10,
            IsActive = true
        };
        var dashboardItem = new NavigationItem
        {
            NavigationGroupId = generalGroup.Id,
            Key = "dashboard",
            LabelEn = "Dashboard",
            LabelUr = "Dashboard",
            Icon = "dashboard",
            Route = string.Empty,
            SortOrder = 10,
            RequiredPermissionsJson = "[]",
            IsActive = true
        };
        var customItem = new NavigationItem
        {
            NavigationGroupId = generalGroup.Id,
            Key = "custom-command-center",
            LabelEn = "Custom Command Center",
            LabelUr = "Custom Command Center",
            Icon = "dashboard",
            Route = "custom/command-center",
            SortOrder = 90,
            RequiredPermissionsJson = "[]",
            IsActive = true
        };

        dbContext.NavigationGroups.Add(generalGroup);
        dbContext.NavigationItems.AddRange(dashboardItem, customItem);
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync(seedDemoData: false, CancellationToken.None);

        var globalGroups = await dbContext.NavigationGroups
            .Where(x => x.TenantId == null)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        var globalItems = await dbContext.NavigationItems
            .Where(x => x.NavigationGroup.TenantId == null)
            .ToListAsync();
        var iconKeys = await dbContext.NavigationIcons
            .Select(x => x.Key)
            .ToListAsync();

        Assert.Contains(globalGroups, x => x.Key == "admin");
        Assert.Contains(globalGroups, x => x.Key == "operations");
        Assert.Contains(globalGroups, x => x.Key == "insights");
        Assert.Contains(globalGroups, x => x.Key == "workspace");
        Assert.Contains(globalItems, x => x.Key == "custom-command-center" && x.Route == "custom/command-center");
        Assert.Contains(globalItems, x => x.Key == "payments" && x.Route == "payments");
        Assert.Contains(globalItems, x => x.Key == "pharmacy-reports" && x.Route == "pharmacy/reports");
        Assert.Contains(globalItems, x => x.Key == "lab-workflow" && x.Route == "lab-workflow");
        Assert.Contains(globalItems, x => x.Key == "timeline" && x.Route == "timeline");
        Assert.Contains(globalItems, x => x.Key == "analytics" && x.Route == "analytics");
        Assert.Contains(globalItems, x => x.Key == "security" && x.Route == "security");
        Assert.Contains(globalItems, x => x.Key == "doctor-discovery" && x.Route == "doctor-discovery");
        Assert.Contains(globalItems, x => x.Key == "help" && x.Route == "help");
        Assert.Contains(globalItems, x => x.Key == "navigation-studio" && x.Route == "admin/navigation-studio");
        Assert.Contains(iconKeys, x => x == "payments");
        Assert.Contains(iconKeys, x => x == "reports");
        Assert.Contains(iconKeys, x => x == "analytics");
        Assert.Contains(iconKeys, x => x == "timeline");
        Assert.Contains(iconKeys, x => x == "security");
        Assert.Contains(iconKeys, x => x == "discovery");
        Assert.Contains(iconKeys, x => x == "help");
        Assert.Contains(iconKeys, x => x == "admin-tools");

        var menuService = new AdminOperationsService(
            dbContext,
            new FakeCurrentUser(Guid.NewGuid(), null, isAuthenticated: true, isSuperAdmin: true, permissions: []));
        var menu = await menuService.GetNavigationMenuAsync("en", CancellationToken.None);

        Assert.True(menu.IsSuccess);
        Assert.Contains(menu.Value.Groups.SelectMany(x => x.Items), x => x.Key == "payments" && x.Route == "payments");
        Assert.Contains(menu.Value.Groups.SelectMany(x => x.Items), x => x.Key == "navigation-studio");
    }

    [Fact]
    public async Task SeedAsync_ShouldCreateGlobalNavigationWhenOnlyTenantSpecificMenuExists()
    {
        await using var dbContext = CreateDbContext();

        var tenantId = Guid.NewGuid();
        dbContext.NavigationGroups.Add(new NavigationGroup
        {
            TenantId = tenantId,
            Key = "tenant-dashboard",
            LabelEn = "Tenant Dashboard",
            LabelUr = "Tenant Dashboard",
            SortOrder = 10,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync(seedDemoData: false, CancellationToken.None);

        Assert.Contains(await dbContext.NavigationGroups.Where(x => x.TenantId == null).ToListAsync(), x => x.Key == "general");
        Assert.Contains(await dbContext.NavigationItems.Where(x => x.NavigationGroup.TenantId == null).ToListAsync(), x => x.Key == "payments");
        Assert.Contains(await dbContext.NavigationGroups.Where(x => x.TenantId == tenantId).ToListAsync(), x => x.Key == "tenant-dashboard");
    }

    [Fact]
    public async Task SeedAsync_ShouldMergeNavigationSystemSettingWithEnterpriseDefaults()
    {
        await using var dbContext = CreateDbContext();

        dbContext.SystemSettings.Add(new SystemSetting
        {
            SettingKey = NavigationDefaults.SettingKey,
            GroupName = "Platform",
            DisplayName = "Navigation menu configuration",
            ValueType = "Json",
            Value =
                """
                {
                  "groups": [
                    {
                      "key": "custom",
                      "sortOrder": 5,
                      "labels": {
                        "en": "Custom",
                        "ur": "Custom"
                      },
                      "items": [
                        {
                          "key": "custom-dashboard",
                          "label": {
                            "en": "Custom Dashboard",
                            "ur": "Custom Dashboard"
                          },
                          "icon": "dashboard",
                          "route": "custom/dashboard",
                          "sortOrder": 10
                        }
                      ]
                    }
                  ]
                }
                """
        });
        await dbContext.SaveChangesAsync();

        var seeder = CreateSeeder(dbContext);
        await seeder.SeedAsync(seedDemoData: false, CancellationToken.None);

        var setting = await dbContext.SystemSettings.SingleAsync(x => x.SettingKey == NavigationDefaults.SettingKey);
        using var document = JsonDocument.Parse(setting.Value);
        var groups = document.RootElement.GetProperty("groups").EnumerateArray().ToList();

        Assert.Contains(groups, x => string.Equals(x.GetProperty("key").GetString(), "custom", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(groups, x => string.Equals(x.GetProperty("key").GetString(), "admin", StringComparison.OrdinalIgnoreCase));

        var operations = groups.Single(x => string.Equals(x.GetProperty("key").GetString(), "operations", StringComparison.OrdinalIgnoreCase));
        var operationsItems = operations.GetProperty("items").EnumerateArray().ToList();

        Assert.Contains(operationsItems, x => string.Equals(x.GetProperty("key").GetString(), "payments", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(operationsItems, x => string.Equals(x.GetProperty("key").GetString(), "lab-workflow", StringComparison.OrdinalIgnoreCase));
    }

    private static DatabaseSeeder CreateSeeder(HealthCareDbContext dbContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SuperAdmin:Email"] = "superadmin@healthcarems.local",
                ["SuperAdmin:Password"] = "ChangeMe@12345"
            })
            .Build();

        return new DatabaseSeeder(
            dbContext,
            new Pbkdf2PasswordHasher(),
            configuration,
            NullLogger<DatabaseSeeder>.Instance);
    }

    private static HealthCareDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HealthCareDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HealthCareDbContext(options);
    }

    private sealed class FakeCurrentUser(
        Guid? userId,
        Guid? tenantId,
        bool isAuthenticated,
        bool isSuperAdmin,
        IReadOnlyCollection<string> permissions) : ICurrentUser
    {
        public Guid? UserId { get; } = userId;

        public Guid? TenantId { get; } = tenantId;

        public bool IsAuthenticated { get; } = isAuthenticated;

        public bool IsSuperAdmin { get; } = isSuperAdmin;

        public IReadOnlyCollection<string> Permissions { get; } = permissions;
    }
}

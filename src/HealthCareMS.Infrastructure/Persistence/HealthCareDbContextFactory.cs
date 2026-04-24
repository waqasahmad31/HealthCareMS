using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthCareMS.Infrastructure.Persistence;

public sealed class HealthCareDbContextFactory : IDesignTimeDbContextFactory<HealthCareDbContext>
{
    public HealthCareDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiProjectPath = ResolveApiProjectPath();
        var connectionString =
            Environment.GetEnvironmentVariable("HealthCareMS_ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? ReadConnectionString(apiProjectPath, "appsettings.json")
            ?? ReadConnectionString(apiProjectPath, $"appsettings.{environmentName}.json")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is required for design-time DbContext creation. Configure it in appsettings.json or HealthCareMS_ConnectionStrings__DefaultConnection.");

        var optionsBuilder = new DbContextOptionsBuilder<HealthCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new HealthCareDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "HealthCareMS.API");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate src/HealthCareMS.API/appsettings.json for design-time DbContext creation.");
    }

    private static string? ReadConnectionString(string apiProjectPath, string fileName)
    {
        var fullPath = Path.Combine(apiProjectPath, fileName);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        using var stream = File.OpenRead(fullPath);
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings))
        {
            return null;
        }

        return connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection)
            ? defaultConnection.GetString()
            : null;
    }
}

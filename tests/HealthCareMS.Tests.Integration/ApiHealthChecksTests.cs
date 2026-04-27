using HealthCareMS.API.Health;
using HealthCareMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace HealthCareMS.Tests.Integration;

public sealed class ApiHealthChecksTests
{
    [Fact]
    public async Task DatabaseReadinessHealthCheck_ShouldReturnHealthy_ForReachableDatabase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<HealthCareDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var serviceProvider = services.BuildServiceProvider();
        var healthCheck = new DatabaseReadinessHealthCheck(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<DatabaseReadinessHealthCheck>.Instance);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Database connectivity is healthy.", result.Description);
    }

    [Fact]
    public async Task HealthCheckResponseWriter_ShouldWriteStructuredJsonPayload()
    {
        var httpContext = new DefaultHttpContext();
        await using var body = new MemoryStream();
        httpContext.Response.Body = body;

        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["self"] = new(
                    status: HealthStatus.Healthy,
                    description: "API process is running.",
                    duration: TimeSpan.FromMilliseconds(10),
                    exception: null,
                    data: new Dictionary<string, object>()),
                ["database"] = new(
                    status: HealthStatus.Healthy,
                    description: "Database connectivity is healthy.",
                    duration: TimeSpan.FromMilliseconds(15),
                    exception: null,
                    data: new Dictionary<string, object>())
            },
            totalDuration: TimeSpan.FromMilliseconds(25));

        await HealthCheckResponseWriter.WriteJsonAsync(httpContext, report);

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        var root = document.RootElement;

        Assert.Equal("HealthCareMS.API", root.GetProperty("service").GetString());
        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);

        var checks = root.GetProperty("checks").EnumerateArray().ToList();
        Assert.Equal(2, checks.Count);
        Assert.Contains(checks, x => x.GetProperty("name").GetString() == "database");
        Assert.Contains(checks, x => x.GetProperty("name").GetString() == "self");
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCareMS.API.Health;

public static class HealthCheckResponseWriter
{
    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            service = "HealthCareMS.API",
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    durationMs = Math.Round(x.Value.Duration.TotalMilliseconds, 2),
                    error = x.Value.Exception?.Message
                })
                .ToArray()
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(payload);
    }
}

using System.Diagnostics;
using System.Diagnostics.Metrics;
using HealthCareMS.API.Configuration;
using Microsoft.Extensions.Options;

namespace HealthCareMS.API.Security;

public sealed class ResponseTimingMiddleware(
    RequestDelegate next,
    ILogger<ResponseTimingMiddleware> logger,
    IOptions<ApiHardeningOptions> hardeningOptions)
{
    private static readonly Meter Meter = new("HealthCareMS.API.Performance", "1.0.0");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "healthcarems.api.request.duration.ms",
        unit: "ms",
        description: "HTTP request duration");

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            var path = context.Request.Path.Value ?? "/";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;

            RequestDuration.Record(elapsedMs, new KeyValuePair<string, object?>("method", method), new KeyValuePair<string, object?>("route", path), new KeyValuePair<string, object?>("status_code", statusCode));

            if (!context.Response.HasStarted)
            {
                context.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString("0");
            }

            var options = hardeningOptions.Value.ResponseTime;
            if (elapsedMs >= options.SlowRequestThresholdMs)
            {
                logger.LogWarning(
                    "Slow request detected. Method={Method} Route={Route} StatusCode={StatusCode} DurationMs={DurationMs} SloTargetMs={SloTargetMs}",
                    method,
                    path,
                    statusCode,
                    elapsedMs,
                    options.SloTargetMs);
            }
        }
    }
}

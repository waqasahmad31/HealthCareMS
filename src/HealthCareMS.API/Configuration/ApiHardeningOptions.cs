namespace HealthCareMS.API.Configuration;

public sealed class ApiHardeningOptions
{
    public const string SectionName = "ApiHardening";

    public RateLimitOptions RateLimiting { get; init; } = new();
    public TelemetryOptions Telemetry { get; init; } = new();
    public ResponseTimeOptions ResponseTime { get; init; } = new();

    public sealed class RateLimitOptions
    {
        public int GlobalPermitLimit { get; init; } = 200;
        public int GlobalWindowSeconds { get; init; } = 60;
        public int AuthPermitLimit { get; init; } = 20;
        public int AuthWindowSeconds { get; init; } = 60;
        public int QueueLimit { get; init; } = 0;
    }

    public sealed class TelemetryOptions
    {
        public string ServiceName { get; init; } = "HealthCareMS.API";
        public string? OtlpEndpoint { get; init; }
    }

    public sealed class ResponseTimeOptions
    {
        public int SlowRequestThresholdMs { get; init; } = 1000;
        public int SloTargetMs { get; init; } = 500;
    }
}

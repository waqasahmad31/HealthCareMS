using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.MemoryStorage;
using HealthCareMS.API.Configuration;
using HealthCareMS.API.Consultations;
using HealthCareMS.API.Health;
using HealthCareMS.API.Hubs;
using HealthCareMS.API.Notifications;
using HealthCareMS.API.Security;
using HealthCareMS.Application;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Insights;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Infrastructure;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Configuration;
using HealthCareMS.Infrastructure.Seed;
using HealthCareMS.Shared.Api;
using HealthCareMS.Shared.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "HealthCareMS.API")
    .WriteTo.Console()
    .WriteTo.File("logs/HealthCareMS-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicGetShort", policy => policy.Expire(TimeSpan.FromSeconds(30)));
    options.AddPolicy("LookupGetMedium", policy => policy.Expire(TimeSpan.FromMinutes(2)));
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("API process is running."),
        tags: ["live", "ready"])
    .AddCheck<DatabaseReadinessHealthCheck>(
        "database",
        tags: ["ready"]);
builder.Services.AddSignalR();
builder.Services.AddHangfire(configuration => configuration.UseMemoryStorage());
builder.Services.AddHangfireServer();
builder.Services.AddScoped<IInAppNotificationPublisher, SignalRInAppNotificationPublisher>();
builder.Services.AddScoped<IConsultationSessionNotifier, SignalRConsultationSessionNotifier>();
builder.Services.AddScoped<IConsultationChatNotifier, SignalRConsultationChatNotifier>();
builder.Services.AddSingleton<IReminderScheduler, HangfireReminderScheduler>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var clientBaseUrl = builder.Configuration[$"{ApplicationLinkOptions.SectionName}:ClientBaseUrl"];
        var origins = configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : string.IsNullOrWhiteSpace(clientBaseUrl)
                ? []
                : [clientBaseUrl];

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var hardeningOptions = builder.Configuration.GetSection(ApiHardeningOptions.SectionName).Get<ApiHardeningOptions>() ?? new ApiHardeningOptions();
builder.Services.Configure<ApiHardeningOptions>(builder.Configuration.GetSection(ApiHardeningOptions.SectionName));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtAuthentication");
                logger.LogWarning(context.Exception, "JWT authentication failed for path {Path}", context.HttpContext.Request.Path.Value);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var hasSub = context.Principal?.Claims.Any(x =>
                    string.Equals(x.Type, "sub", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", StringComparison.OrdinalIgnoreCase)) == true;
                if (!hasSub)
                {
                    context.Fail("Required subject claim is missing.");
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddLocalization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var payload = ApiResponse<EmptyResponse>.Fail(
            new Error("RATE_LIMIT_EXCEEDED", "Too many requests. Please retry later."),
            context.HttpContext.TraceIdentifier);
        await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(payload), cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var partitionKey = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = hardeningOptions.RateLimiting.GlobalPermitLimit,
                Window = TimeSpan.FromSeconds(hardeningOptions.RateLimiting.GlobalWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = hardeningOptions.RateLimiting.QueueLimit
            });
    });

    options.AddPolicy("auth-policy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            $"auth:{ip}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = hardeningOptions.RateLimiting.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(hardeningOptions.RateLimiting.AuthWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = hardeningOptions.RateLimiting.QueueLimit
            });
    });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(hardeningOptions.Telemetry.ServiceName))
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(hardeningOptions.Telemetry.OtlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(hardeningOptions.Telemetry.OtlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(hardeningOptions.Telemetry.OtlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(hardeningOptions.Telemetry.OtlpEndpoint));
        }
    });

var app = builder.Build();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ur") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "HealthCareMS API v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseHangfireDashboard("/hangfire");
}

RecurringJob.AddOrUpdate<IPharmacyService>(
    "PharmacyStockAlertScan",
    service => service.RunStockAlertScanAsync(CancellationToken.None),
    Cron.Daily);

RecurringJob.AddOrUpdate<IInsightService>(
    "AnalyticsDigestEmail",
    service => service.SendScheduledAnalyticsEmailsAsync(CancellationToken.None),
    Cron.Daily);

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    var seedDemoData = app.Configuration.GetValue("Database:SeedDemoData", app.Environment.IsDevelopment());
    await app.Services.SeedHealthCareDatabaseAsync(seedDemoData);
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? "/");
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
    };
});
app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseOutputCache();
app.UseRateLimiter();
app.UseMiddleware<ResponseTimingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
        "/health",
        new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
        })
    .AllowAnonymous();
app.MapHealthChecks(
        "/health/live",
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
            ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
        })
    .AllowAnonymous();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = HealthCheckResponseWriter.WriteJsonAsync
        })
    .AllowAnonymous();
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ConsultationHub>("/hubs/consultations");
app.MapHub<ConsultationChatHub>("/hubs/consultation-chat");

await app.RunAsync();

using System.Text;
using System.Text.Json;
using System.Globalization;
using Hangfire;
using Hangfire.MemoryStorage;
using HealthCareMS.API.Consultations;
using HealthCareMS.API.Hubs;
using HealthCareMS.API.Notifications;
using HealthCareMS.API.Security;
using HealthCareMS.Application;
using HealthCareMS.Application.Abstractions.Tenancy;
using HealthCareMS.Application.Consultations;
using HealthCareMS.Application.Notifications;
using HealthCareMS.Application.Pharmacy;
using HealthCareMS.Infrastructure;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
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
builder.Services.AddHealthChecks();
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
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["https://localhost:5002"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
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
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddLocalization();

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
    app.UseHangfireDashboard("/hangfire");
}

RecurringJob.AddOrUpdate<IPharmacyService>(
    "PharmacyStockAlertScan",
    service => service.RunStockAlertScanAsync(CancellationToken.None),
    Cron.Daily);

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    var seedDemoData = app.Configuration.GetValue("Database:SeedDemoData", app.Environment.IsDevelopment());
    await app.Services.SeedHealthCareDatabaseAsync(seedDemoData);
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<QueueHub>("/hubs/queue");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<ConsultationHub>("/hubs/consultations");
app.MapHub<ConsultationChatHub>("/hubs/consultation-chat");

await app.RunAsync();

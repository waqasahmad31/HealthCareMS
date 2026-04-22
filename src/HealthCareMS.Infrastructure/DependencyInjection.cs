using HealthCareMS.Application.Abstractions.Authentication;
using HealthCareMS.Application.Abstractions.Persistence;
using HealthCareMS.Application.Doctors;
using HealthCareMS.Application.Patients;
using HealthCareMS.Application.Tenants;
using HealthCareMS.Infrastructure.Authentication;
using HealthCareMS.Infrastructure.Doctors;
using HealthCareMS.Infrastructure.Patients;
using HealthCareMS.Infrastructure.Persistence;
using HealthCareMS.Infrastructure.Seed;
using HealthCareMS.Infrastructure.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthCareMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=HealthCareMS;Username=postgres;Password=123456789";

        services.AddDbContext<HealthCareDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(options =>
            configuration.GetSection(JwtOptions.SectionName).Bind(options));

        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<HealthCareDbContext>());
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

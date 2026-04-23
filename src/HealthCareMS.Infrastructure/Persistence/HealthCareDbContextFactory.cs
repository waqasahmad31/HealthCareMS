using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthCareMS.Infrastructure.Persistence;

public sealed class HealthCareDbContextFactory : IDesignTimeDbContextFactory<HealthCareDbContext>
{
    public HealthCareDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HealthCareMS_ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Environment variable 'HealthCareMS_ConnectionStrings__DefaultConnection' is required for design-time DbContext creation.");

        var optionsBuilder = new DbContextOptionsBuilder<HealthCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new HealthCareDbContext(optionsBuilder.Options);
    }
}

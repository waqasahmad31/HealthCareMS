using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthCareMS.Infrastructure.Persistence;

public sealed class HealthCareDbContextFactory : IDesignTimeDbContextFactory<HealthCareDbContext>
{
    public HealthCareDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("HealthCareMS_ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=HealthCareMS;Username=postgres;Password=123456789";

        var optionsBuilder = new DbContextOptionsBuilder<HealthCareDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new HealthCareDbContext(optionsBuilder.Options);
    }
}

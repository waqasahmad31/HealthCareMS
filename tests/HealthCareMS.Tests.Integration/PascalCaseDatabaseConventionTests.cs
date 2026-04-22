using HealthCareMS.Infrastructure.Persistence;

namespace HealthCareMS.Tests.Integration;

public sealed class PascalCaseDatabaseConventionTests
{
    [Theory]
    [InlineData(DatabaseSchemas.Identity)]
    [InlineData(DatabaseSchemas.Patient)]
    [InlineData(DatabaseSchemas.Doctor)]
    [InlineData(DatabaseSchemas.Appointment)]
    [InlineData(DatabaseSchemas.Pharmacy)]
    [InlineData(DatabaseSchemas.Lab)]
    [InlineData(DatabaseSchemas.Payment)]
    [InlineData(DatabaseSchemas.Notification)]
    [InlineData(DatabaseSchemas.Audit)]
    [InlineData(DatabaseSchemas.Files)]
    public void DatabaseSchemaNames_ShouldBePascalCase(string schemaName)
    {
        Assert.True(char.IsUpper(schemaName[0]));
        Assert.DoesNotContain("_", schemaName);
    }
}

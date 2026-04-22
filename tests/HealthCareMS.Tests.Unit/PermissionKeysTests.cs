using HealthCareMS.Domain.Identity;

namespace HealthCareMS.Tests.Unit;

public sealed class PermissionKeysTests
{
    [Fact]
    public void All_ShouldIncludeSuperAdminWildcard()
    {
        Assert.Contains(PermissionKeys.System.SuperAdminAll, PermissionKeys.All);
    }

    [Fact]
    public void All_ShouldNotContainDuplicates()
    {
        var duplicates = PermissionKeys.All
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }
}

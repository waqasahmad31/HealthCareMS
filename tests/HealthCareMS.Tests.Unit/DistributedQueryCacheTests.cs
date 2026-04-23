using HealthCareMS.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HealthCareMS.Tests.Unit;

public sealed class DistributedQueryCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldReuseCachedValue_AndInvalidateNamespace()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var queryCache = new DistributedQueryCache(cache);

        var factoryRuns = 0;
        var first = await queryCache.GetOrCreateAsync(
            "tests",
            "key-1",
            TimeSpan.FromMinutes(1),
            _ =>
            {
                factoryRuns++;
                return Task.FromResult("value-1");
            },
            CancellationToken.None);

        var second = await queryCache.GetOrCreateAsync(
            "tests",
            "key-1",
            TimeSpan.FromMinutes(1),
            _ =>
            {
                factoryRuns++;
                return Task.FromResult("value-2");
            },
            CancellationToken.None);

        await queryCache.InvalidateNamespaceAsync("tests", CancellationToken.None);

        var third = await queryCache.GetOrCreateAsync(
            "tests",
            "key-1",
            TimeSpan.FromMinutes(1),
            _ =>
            {
                factoryRuns++;
                return Task.FromResult("value-3");
            },
            CancellationToken.None);

        Assert.Equal("value-1", first);
        Assert.Equal("value-1", second);
        Assert.Equal("value-3", third);
        Assert.Equal(2, factoryRuns);
    }
}

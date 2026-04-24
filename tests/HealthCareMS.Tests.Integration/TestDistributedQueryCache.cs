using HealthCareMS.Infrastructure.Caching;

namespace HealthCareMS.Tests.Integration;

internal sealed class TestDistributedQueryCache : IDistributedQueryCache
{
    public Task<T> GetOrCreateAsync<T>(
        string cacheNamespace,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        _ = cacheNamespace;
        _ = cacheKey;
        _ = ttl;
        return factory(cancellationToken);
    }

    public Task InvalidateNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken)
    {
        _ = cacheNamespace;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}

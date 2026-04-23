namespace HealthCareMS.Infrastructure.Caching;

public interface IDistributedQueryCache
{
    Task<T> GetOrCreateAsync<T>(
        string cacheNamespace,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken);

    Task InvalidateNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken);
}

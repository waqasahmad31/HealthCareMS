using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace HealthCareMS.Infrastructure.Caching;

public sealed class DistributedQueryCache(IDistributedCache cache) : IDistributedQueryCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string cacheNamespace,
        string cacheKey,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        var version = await GetNamespaceVersionAsync(cacheNamespace, cancellationToken);
        var namespacedKey = BuildDataKey(cacheNamespace, version, cacheKey);

        var payload = await cache.GetAsync(namespacedKey, cancellationToken);
        if (payload is { Length: > 0 })
        {
            var cached = JsonSerializer.Deserialize<T>(payload, SerializerOptions);
            if (cached is not null)
            {
                return cached;
            }
        }

        var value = await factory(cancellationToken);
        var serialized = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        await cache.SetAsync(
            namespacedKey,
            serialized,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            },
            cancellationToken);

        return value;
    }

    public async Task InvalidateNamespaceAsync(string cacheNamespace, CancellationToken cancellationToken)
    {
        var versionKey = BuildVersionKey(cacheNamespace);
        await cache.SetStringAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            },
            cancellationToken);
    }

    private async Task<string> GetNamespaceVersionAsync(string cacheNamespace, CancellationToken cancellationToken)
    {
        var versionKey = BuildVersionKey(cacheNamespace);
        var version = await cache.GetStringAsync(versionKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        version = "v1";
        await cache.SetStringAsync(
            versionKey,
            version,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
            },
            cancellationToken);
        return version;
    }

    private static string BuildVersionKey(string cacheNamespace) => $"cache:{cacheNamespace}:version";

    private static string BuildDataKey(string cacheNamespace, string version, string cacheKey) => $"cache:{cacheNamespace}:{version}:{cacheKey}";
}

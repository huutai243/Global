using System.Text.Json;
using ECommerce.Core.SharedLibs.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace ECommerce.Infrastructure.Redis;

public sealed class RedisProductCache(IDistributedCache distributedCache) : IProductCache
{
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await distributedCache.GetStringAsync(key, cancellationToken);
        return payload is null ? default : JsonSerializer.Deserialize<TValue>(payload);
    }

    public Task SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        return distributedCache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration },
            cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return distributedCache.RemoveAsync(key, cancellationToken);
    }
}

using ECommerce.Shared.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Redis;

public sealed class RedisProductCache : IProductCache
{
    private readonly IDistributedCache _distributedCache;
    private readonly IJsonHelper _jsonHelper;
    private readonly RedisSettings _redisSettings;

    public RedisProductCache(
        IDistributedCache distributedCache,
        IJsonHelper jsonHelper,
        IOptions<RedisSettings> redisOptions)
    {
        _distributedCache = distributedCache;
        _jsonHelper = jsonHelper;
        _redisSettings = redisOptions.Value;
    }

    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default)
    {
        var payload = await _distributedCache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return _jsonHelper.Deserialize<TValue>(payload);
    }

    public async Task SetAsync<TValue>(string key, TValue value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            return;
        }

        var payload = _jsonHelper.Serialize(value);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(
                _redisSettings.DefaultExpirationInMinutes)
        };

        await _distributedCache.SetStringAsync(key, payload, cacheOptions, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task<TValue?> GetOrSetAsync<TValue>(
        string key,
        Func<CancellationToken, Task<TValue?>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<TValue>(key, cancellationToken);

        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory(cancellationToken);

        if (value is null)
        {
            return default;
        }

        await SetAsync(key, value, expiration, cancellationToken);

        return value;
    }
}
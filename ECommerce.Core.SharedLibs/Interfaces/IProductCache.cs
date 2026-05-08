namespace ECommerce.Core.SharedLibs.Interfaces;

public interface IProductCache
{
    Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<TValue>(string key, TValue value, TimeSpan expiration, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

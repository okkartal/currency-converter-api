using System.Text.Json;
using CurrencyConverter.Core.Contracts; 
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Infrastructure.Services;

public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(IDistributedCache cache, ILogger<DistributedCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cachedData = await _cache.GetStringAsync(key, cancellationToken);

        if (!string.IsNullOrEmpty(cachedData))
        {
            _logger.LogDebug($"Cache hit for key {key}");
            return JsonSerializer.Deserialize<T>(cachedData);
        }

        _logger.LogDebug($"Cache miss for key {key}");

        var result = await factory();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        var serializedData = JsonSerializer.Serialize(result);
        await _cache.SetStringAsync(key, serializedData, options, cancellationToken);

        return result;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
        _logger.LogDebug($"Removed key {key} from cache");
    }
}
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;

namespace ProductService.Infrastructure.Caching;

/// <summary>
/// Redis Cache implementasyonu. Cache-Aside Pattern + Cache Invalidation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    { _cache = cache; _logger = logger; }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var data = await _cache.GetStringAsync(key, ct);
            if (string.IsNullOrEmpty(data)) return default;
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis okuma hatası: {Key}", key); return default; }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            { AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5) };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis yazma hatası: {Key}", key); }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _cache.RemoveAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis silme hatası: {Key}", key); }
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken ct = default)
    {
        _logger.LogInformation("Cache invalidation: {Prefix}", prefixKey);
        for (int i = 1; i <= 10; i++)
        {
            await RemoveAsync($"{prefixKey}{i}:10:all", ct);
            await RemoveAsync($"{prefixKey}{i}:20:all", ct);
        }
    }
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace taskedin_be.src.Modules.Common.Services;

/// <summary>
/// Service for caching frequently accessed data.
/// Provides a simple interface for common caching operations.
/// </summary>
public interface ICacheService
    {
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class;
        Task<T> GetOrSetValueAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : struct;
        T? GetOrSet<T>(string key, Func<T> getItem, TimeSpan? expiration = null) where T : class;
        T GetOrSetValue<T>(string key, Func<T> getItem, TimeSpan? expiration = null) where T : struct;
        void Remove(string key);
        void Clear();
    }

public class CacheService(IMemoryCache cache) : ICacheService
{
    private readonly IMemoryCache _cache = cache;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(15);
    // Track all cache keys we've added so we can clear them
    private readonly ConcurrentDictionary<string, bool> _trackedKeys = new();

    /// <summary>
    /// Gets an item from cache, or sets it if not found (async).
    /// </summary>
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : class
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue;
        }

        var value = await getItem();
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = expiration == null ? TimeSpan.FromMinutes(5) : null, // Sliding only if using default
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(key, value, cacheOptions);
            _trackedKeys.TryAdd(key, true);
        }

        return value;
    }

    /// <summary>
    /// Gets a value type from cache, or sets it if not found (async).
    /// </summary>
    public async Task<T> GetOrSetValueAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null) where T : struct
    {
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            return cachedValue;
        }

        var value = await getItem();
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            SlidingExpiration = expiration == null ? TimeSpan.FromMinutes(5) : null,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, cacheOptions);
        _trackedKeys.TryAdd(key, true);
        return value;
    }

    /// <summary>
    /// Gets an item from cache, or sets it if not found (sync).
    /// </summary>
    public T? GetOrSet<T>(string key, Func<T> getItem, TimeSpan? expiration = null) where T : class
    {
        if (_cache.TryGetValue(key, out T? cachedValue))
        {
            return cachedValue;
        }

        var value = getItem();
        if (value != null)
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
                SlidingExpiration = expiration == null ? TimeSpan.FromMinutes(5) : null,
                Priority = CacheItemPriority.Normal
            };

            _cache.Set(key, value, cacheOptions);
            _trackedKeys.TryAdd(key, true);
        }

        return value;
    }

    /// <summary>
    /// Gets a value type from cache, or sets it if not found (sync).
    /// </summary>
    public T GetOrSetValue<T>(string key, Func<T> getItem, TimeSpan? expiration = null) where T : struct
    {
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            return cachedValue;
        }

        var value = getItem();
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            SlidingExpiration = expiration == null ? TimeSpan.FromMinutes(5) : null,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, cacheOptions);
        _trackedKeys.TryAdd(key, true);
        return value;
    }

    /// <summary>
    /// Removes an item from cache.
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all cache entries tracked by this service (use with caution).
    /// Note: IMemoryCache doesn't have a built-in Clear() method, so we track keys
    /// and remove them individually. This only clears keys added through this service.
    /// </summary>
    public void Clear()
    {
        // Remove all tracked keys
        foreach (var key in _trackedKeys.Keys)
        {
            _cache.Remove(key);
        }
        
        // Clear the tracking dictionary
        _trackedKeys.Clear();
    }
}


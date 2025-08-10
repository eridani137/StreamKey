using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Infrastructure;

public class InMemorySettingsStorage(IMemoryCache cache) : ISettingsStorage
{
    private readonly ConcurrentDictionary<string, byte> _keyTracker = new();
    
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        Priority = CacheItemPriority.NeverRemove,
        SlidingExpiration = null,
        AbsoluteExpiration = null
    };
    
    public Task<T?> GetSettingAsync<T>(string key, T defaultValue) where T : class
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (cache.TryGetValue(key, out var value) && value is T typedValue)
        {
            return Task.FromResult<T?>(typedValue);
        }

        cache.Set(key, defaultValue, CacheOptions);
        _keyTracker.TryAdd(key, 0);

        return Task.FromResult<T?>(defaultValue);
    }
    
    public Task<bool> GetBoolSettingAsync(string key, bool defaultValue)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        if (cache.TryGetValue(key, out var value) && value is bool boolValue)
        {
            return Task.FromResult(boolValue);
        }

        cache.Set(key, defaultValue, CacheOptions);
        _keyTracker.TryAdd(key, 0);

        return Task.FromResult(defaultValue);
    }
    
    public Task SetBoolSettingAsync(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        cache.Set(key, value, CacheOptions);
        _keyTracker.TryAdd(key, 0);
        
        return Task.CompletedTask;
    }

    public Task SetSettingAsync<T>(string key, T value) where T : class
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
        
        if (value == null) 
            throw new ArgumentNullException(nameof(value));

        cache.Set(key, value, CacheOptions);
        _keyTracker.TryAdd(key, 0);
        
        return Task.CompletedTask;
    }

    public Task RemoveSettingAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        cache.Remove(key);
        _keyTracker.TryRemove(key, out _);
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrEmpty(key)) 
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return Task.FromResult(cache.TryGetValue(key, out _));
    }

    public Task<IEnumerable<string>> GetAllKeysAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_keyTracker.Keys.ToList());
    }
}
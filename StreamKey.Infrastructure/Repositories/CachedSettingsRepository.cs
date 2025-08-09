using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class CachedSettingsRepository(SettingsRepository repository, IMemoryCache cache)
    : BaseCachedRepository<SettingsEntity, SettingsRepository>(repository, cache), ISettingsRepository
{
    protected override string CacheKeyPrefix => "Setting";
    
    public Task<Dictionary<string, object>?> GetAll()
    {
        return GetCachedData(GetCacheKey(), Repository.GetAll, TimeSpan.FromHours(24));
    }

    public async Task<T> SetValue<T>(string key, T value)
    {
        var result = await Repository.SetValue(key, value);
        InvalidateKeyCache();
        return result;
    }

    public Task<T?> GetValue<T>(string key)
    {
        return GetCachedData(GetCacheKey(), () => Repository.GetValue<T>(key), TimeSpan.FromHours(24));
    }

    public async Task Remove(string key)
    {
        var entity = Repository.GetSet().FirstOrDefault(x => x.Key == key);
        if (entity is not null)
        {
            Delete(entity);
            await Repository.Save();
            InvalidateKeyCache();
        }
    }
}
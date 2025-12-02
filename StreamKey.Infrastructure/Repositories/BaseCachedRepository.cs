using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public abstract class BaseCachedRepository<TEntity, TRepository>(TRepository repository, IMemoryCache cache)
    where TEntity : BaseEntity
    where TRepository : IBaseRepository<TEntity>
{
    protected readonly TRepository Repository = repository;
    protected abstract string CacheKeyPrefix { get; }

    protected string GetCacheKey(string? suffix = null)
    {
        return suffix is null
            ? CacheKeyPrefix
            : $"{CacheKeyPrefix}:{suffix}";
    }

    protected void InvalidateCache(string? suffix = null)
    {
        InvalidateKeyCache();
        if (suffix is not null)
        {
            InvalidateKeyCache(suffix);
        }
    }

    private void InvalidateKeyCache()
    {
        cache.Remove(GetCacheKey());
    }

    private void InvalidateKeyCache(string suffix)
    {
        cache.Remove(GetCacheKey(suffix));
    }

    protected async Task<List<TResult>> GetCachedData<TResult>(
        string cacheKey,
        Func<Task<List<TResult>>> dataLoader,
        TimeSpan? absoluteExpiration = null)
    {
        var result = await cache.GetOrCreateAsync(cacheKey, entry =>
        {
            if (absoluteExpiration is not null)
            {
                entry.SetAbsoluteExpiration((TimeSpan)absoluteExpiration);
            }

            return dataLoader();
        });

        return result ?? [];
    }

    protected async Task<TResult?> GetCachedData<TResult>(
        string cacheKey,
        Func<Task<TResult>> dataLoader,
        TimeSpan? absoluteExpiration = null)
    {
        var result = await cache.GetOrCreateAsync(cacheKey, entry =>
        {
            if (absoluteExpiration is not null)
            {
                entry.SetAbsoluteExpiration((TimeSpan)absoluteExpiration);
            }

            return dataLoader();
        });

        return result;
    }
}
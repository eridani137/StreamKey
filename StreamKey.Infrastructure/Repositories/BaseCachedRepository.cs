using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public abstract class BaseCachedRepository<TEntity, TRepository>(TRepository repository, IMemoryCache cache)
    : IBaseRepository<TEntity>
    where TEntity : BaseEntity
    where TRepository : IBaseRepository<TEntity>
{
    protected readonly TRepository Repository = repository;
    protected abstract string CacheKeyPrefix { get; }

    protected string GetCacheKey(string suffix = "") =>
        string.IsNullOrEmpty(suffix) ? CacheKeyPrefix : $"{CacheKeyPrefix}:{suffix}";

    private void InvalidateKeyCache()
    {
        cache.Remove(GetCacheKey());
    }

    public DbSet<TEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(TEntity entity)
    {
        InvalidateKeyCache();
        return Repository.Add(entity);
    }

    public void Update(TEntity entity)
    {
        InvalidateKeyCache();
        Repository.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        InvalidateKeyCache();
        Repository.Delete(entity);
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedButtonRepository(ButtonRepository repository, IMemoryCache cache)
    : BaseCachedRepository<ButtonEntity, ButtonRepository>(repository, cache), IButtonRepository
{
    protected override string CacheKeyPrefix => "Button";
 
    public Task<List<ButtonEntity>> GetAll(CancellationToken cancellationToken)
    {
        return GetCachedData(GetCacheKey(), () => Repository.GetAll(cancellationToken));
    }

    public Task<bool> HasEntity(string link, CancellationToken cancellationToken)
    {
        return Repository.HasEntity(link, cancellationToken);
    }

    public Task<ButtonEntity?> GetByLink(string link, CancellationToken cancellationToken)
    {
        return Repository.GetByLink(link, cancellationToken);
    }

    public DbSet<ButtonEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(ButtonEntity entity, CancellationToken cancellationToken)
    {
        InvalidateCache(entity.Id.ToString());
        return Repository.Add(entity, cancellationToken);
    }

    public Task AddRange(IEnumerable<ButtonEntity> entities, CancellationToken cancellationToken)
    {
        InvalidateCache();
        return Repository.AddRange(entities, cancellationToken);
    }

    public void Update(ButtonEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Update(entity);
    }

    public void Delete(ButtonEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Delete(entity);
    }
}
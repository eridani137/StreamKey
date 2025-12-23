using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedChannelButtonRepository(ChannelButtonRepository repository, IMemoryCache cache)
    : BaseCachedRepository<ChannelButtonEntity, ChannelButtonRepository>(repository, cache), IChannelButtonRepository
{
    protected override string CacheKeyPrefix => "ChannelButton";
 
    public Task<List<ChannelButtonEntity>> GetAll(CancellationToken cancellationToken)
    {
        return GetCachedData(GetCacheKey(), () => Repository.GetAll(cancellationToken));
    }
    
    public DbSet<ChannelButtonEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(ChannelButtonEntity entity, CancellationToken cancellationToken)
    {
        InvalidateCache(entity.Id.ToString());
        return Repository.Add(entity, cancellationToken);
    }

    public Task AddRange(IEnumerable<ChannelButtonEntity> entities, CancellationToken cancellationToken)
    {
        InvalidateCache();
        return Repository.AddRange(entities, cancellationToken);
    }

    public void Update(ChannelButtonEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Update(entity);
    }

    public void Delete(ChannelButtonEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Delete(entity);
    }
}
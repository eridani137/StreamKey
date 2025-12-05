using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedChannelRepository(ChannelRepository repository, IMemoryCache cache)
    : BaseCachedRepository<ChannelEntity, ChannelRepository>(repository, cache), IChannelRepository
{
    protected override string CacheKeyPrefix => "Channel";
    
    public Task<List<ChannelEntity>> GetAll(CancellationToken cancellationToken)
    {
        return GetCachedData(GetCacheKey(), () => Repository.GetAll(cancellationToken));
    }

    public Task<bool> HasEntity(string channelName, CancellationToken cancellationToken)
    {
        return Repository.HasEntity(channelName, cancellationToken);
    }

    public Task<ChannelEntity?> GetByName(string channelName, CancellationToken cancellationToken)
    {
        return Repository.GetByName(channelName, cancellationToken);
    }

    public Task<ChannelEntity?> GetByPosition(int position, CancellationToken cancellationToken)
    {
        return Repository.GetByPosition(position, cancellationToken);
    }

    public Task<bool> HasInPosition(int position, CancellationToken cancellationToken)
    {
        return Repository.HasInPosition(position, cancellationToken);
    }

    public DbSet<ChannelEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(ChannelEntity entity, CancellationToken cancellationToken)
    {
        InvalidateCache(entity.Id.ToString());
        return Repository.Add(entity, cancellationToken);
    }

    public Task AddRange(IEnumerable<ChannelEntity> entities, CancellationToken cancellationToken)
    {
        InvalidateCache();
        return Repository.AddRange(entities, cancellationToken);
    }

    public void Update(ChannelEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Update(entity);
    }

    public void Delete(ChannelEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        Repository.Delete(entity);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedChannelRepository(ChannelRepository repository, IMemoryCache cache)
    : BaseCachedRepository<ChannelEntity, ChannelRepository>(repository, cache), IChannelRepository
{
    protected override string CacheKeyPrefix => "Channel";
    
    public Task<List<ChannelEntity>> GetAll()
    {
        return GetCachedData(GetCacheKey(), Repository.GetAll);
    }

    public Task<bool> HasEntity(string channelName)
    {
        return Repository.HasEntity(channelName);
    }

    public Task<ChannelEntity?> GetByName(string channelName)
    {
        return Repository.GetByName(channelName);
    }

    public Task<ChannelEntity?> GetByPosition(int position)
    {
        return Repository.GetByPosition(position);
    }

    public Task<bool> HasInPosition(int position)
    {
        return Repository.HasInPosition(position);
    }

    public DbSet<ChannelEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(ChannelEntity entity)
    {
        InvalidateCache(entity.Id.ToString());
        return Repository.Add(entity);
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
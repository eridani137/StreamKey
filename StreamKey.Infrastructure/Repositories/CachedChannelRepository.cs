using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class CachedChannelRepository(ChannelRepository repository, IMemoryCache cache)
    : BaseCachedRepository<ChannelEntity, ChannelRepository>(repository, cache), IChannelRepository
{
    protected override string CacheKeyPrefix => "Channel";

    public async Task Create(ChannelEntity channel)
    {
        await Add(channel);
        await Repository.Save();
    }

    public async Task Remove(ChannelEntity channel)
    {
        Delete(channel);
        await Repository.Save();
    }

    async Task IChannelRepository.Update(ChannelEntity channel)
    {
        Update(channel);
        await Repository.Save();
    }

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
}
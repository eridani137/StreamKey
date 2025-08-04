using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IChannelRepository
{
    Task Create(ChannelEntity channel);
    Task Remove(ChannelEntity channel);
    Task<List<ChannelEntity>> GetAll();
    Task<bool> HasEntity(string channelName);
    Task<ChannelEntity?> GetByName(string channelName);
}
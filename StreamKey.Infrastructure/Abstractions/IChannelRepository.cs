using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IChannelRepository : IBaseRepository<ChannelEntity>
{
    Task<List<ChannelEntity>> GetAll();
    Task<bool> HasEntity(string channelName);
    Task<ChannelEntity?> GetByName(string channelName);
    Task<ChannelEntity?> GetByPosition(int position);
    Task<bool> HasInPosition(int position);
}
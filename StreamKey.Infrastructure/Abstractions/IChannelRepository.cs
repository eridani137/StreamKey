using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IChannelRepository : IBaseRepository<ChannelEntity>
{
    Task<List<ChannelEntity>> GetAll(CancellationToken cancellationToken);
    Task<bool> HasEntity(string channelName, CancellationToken cancellationToken);
    Task<ChannelEntity?> GetByName(string channelName, CancellationToken cancellationToken);
    Task<ChannelEntity?> GetByPosition(int position, CancellationToken cancellationToken);
    Task<bool> HasInPosition(int position, CancellationToken cancellationToken);
}
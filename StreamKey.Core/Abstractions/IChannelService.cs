using StreamKey.Core.Results;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Abstractions;

public interface IChannelService
{
    Task<List<ChannelEntity>> GetChannels(CancellationToken cancellationToken);
    Task<Result<ChannelEntity>> AddChannel(ChannelDto dto, CancellationToken cancellationToken);
    Task<Result<ChannelEntity>> RemoveChannel(int position, CancellationToken cancellationToken);
    Task<Result<ChannelEntity>> UpdateChannel(ChannelDto dto, CancellationToken cancellationToken);
    Task UpdateChannelInfo(ChannelEntity entity, CancellationToken cancellationToken);
}
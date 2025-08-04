using StreamKey.Core.DTOs;
using StreamKey.Core.Results;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Abstractions;

public interface IChannelService
{
    Task<List<ChannelEntity>> GetChannels();
    Task<Result<ChannelEntity>> AddChannel(ChannelDto dto);
    Task<Result<ChannelEntity>> RemoveChannel(string channelName);
}
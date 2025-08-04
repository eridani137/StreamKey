using StreamKey.Application.DTOs;
using StreamKey.Application.Entities;
using StreamKey.Application.Results;

namespace StreamKey.Application.Interfaces;

public interface IChannelService
{
    Task<List<ChannelEntity>> GetChannels();
    Task<Result<ChannelEntity>> AddChannel(ChannelDto dto);
    Task<Result<ChannelEntity>> RemoveChannel(string channelName);
}
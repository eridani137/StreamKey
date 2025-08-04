using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ChannelService(CachedChannelRepository channelRepository) : IChannelService
{
    public async Task<List<ChannelEntity>> GetChannels()
    {
        return await channelRepository.GetAll();
    }

    public async Task<Result<ChannelEntity>> AddChannel(ChannelDto dto)
    {
        if (await channelRepository.HasEntity(dto.ChannelName))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelAlreadyExist);
        }

        var channel = new ChannelEntity()
        {
            Name = dto.ChannelName
        };

        await channelRepository.Create(channel);

        return Result.Success(channel);
    }

    public async Task<Result<ChannelEntity>> RemoveChannel(string channelName)
    {
        var channel = await channelRepository.GetByName(channelName);
        if (channel is null)
        {
            return Result.Failure<ChannelEntity>(Error.ChannelNotFound);
        }

        await channelRepository.Remove(channel);

        return Result.Success(channel);
    }
}
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ChannelService(IChannelRepository channelRepository, ICamoufoxService camoufox) : IChannelService
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

        if (await channelRepository.HasInPosition(dto.Position))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelPositionIsBusy);
        }

        var channel = new ChannelEntity()
        {
            Name = dto.ChannelName,
            Position = dto.Position
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

    public async Task<Result<ChannelEntity>> UpdateChannel(ChannelDto dto)
    {
        var channel = await channelRepository.GetByName(dto.ChannelName);
        if (channel is null)
        {
            return Result.Failure<ChannelEntity>(Error.ChannelNotFound);
        }

        if (await channelRepository.HasInPosition(dto.Position))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelPositionIsBusy);
        }

        channel.Position = dto.Position;

        await channelRepository.Update(channel);

        return Result.Success(channel);
    }
}
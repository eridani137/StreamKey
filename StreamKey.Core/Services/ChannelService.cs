using Microsoft.EntityFrameworkCore;
using StreamKey.Application.DTOs;
using StreamKey.Application.Entities;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public class ChannelService(IBaseRepository<ChannelEntity> channelRepository) : IChannelService
{
    public async Task<List<ChannelEntity>> GetChannels()
    {
        return await channelRepository.GetSet().AsQueryable().ToListAsync();
    }

    public async Task<Result<ChannelEntity>> AddChannel(ChannelDto dto)
    {
        if (await channelRepository.GetSet().AsQueryable().AnyAsync(c => c.Name == dto.ChannelName))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelAlreadyExist);
        }

        var channel = new ChannelEntity()
        {
            Name = dto.ChannelName
        };
        
        await channelRepository.Add(channel);
        await channelRepository.Save();

        return Result.Success(channel);
    }

    public async Task<Result<ChannelEntity>> RemoveChannel(string channelName)
    {
        var channel = await channelRepository.GetSet()
            .AsQueryable()
            .FirstOrDefaultAsync(c => c.Name == channelName);
        if (channel is null)
        {
            return Result.Failure<ChannelEntity>(Error.ChannelNotFound);
        }

        channelRepository.Delete(channel);
        await channelRepository.Save();

        return Result.Success(channel);
    }
}
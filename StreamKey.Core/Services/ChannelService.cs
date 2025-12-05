using Microsoft.Extensions.Logging;
using ParserExtension;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Mappers;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ChannelService(
    IChannelRepository channelRepository,
    IUnitOfWork unitOfWork,
    ICamoufoxService camoufox,
    ILogger<ChannelService> logger) : IChannelService
{
    public async Task<List<ChannelEntity>> GetChannels(CancellationToken cancellationToken)
    {
        return await channelRepository.GetAll(cancellationToken);
    }

    public async Task<Result<ChannelEntity>> AddChannel(ChannelDto dto, CancellationToken cancellationToken)
    {
        if (await channelRepository.HasEntity(dto.ChannelName, cancellationToken))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelAlreadyExist);
        }

        if (await channelRepository.HasInPosition(dto.Position, cancellationToken))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelPositionIsBusy);
        }

        var channel = dto.Map();

        await channelRepository.Add(channel, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(channel);
    }

    public async Task<Result<ChannelEntity>> RemoveChannel(int position, CancellationToken cancellationToken)
    {
        var channel = await channelRepository.GetByPosition(position, cancellationToken);
        if (channel is null)
        {
            return Result.Failure<ChannelEntity>(Error.ChannelNotFound);
        }

        channelRepository.Delete(channel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(channel);
    }

    public async Task<Result<ChannelEntity>> UpdateChannel(ChannelDto dto, CancellationToken cancellationToken)
    {
        var channel = await channelRepository.GetByName(dto.ChannelName, cancellationToken);
        if (channel is null)
        {
            return Result.Failure<ChannelEntity>(Error.ChannelNotFound);
        }

        if (await channelRepository.HasInPosition(dto.Position, cancellationToken))
        {
            return Result.Failure<ChannelEntity>(Error.ChannelPositionIsBusy);
        }

        channel.Position = dto.Position;

        channelRepository.Update(channel);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(channel);
    }

    public async Task UpdateChannelInfo(ChannelEntity channel, CancellationToken cancellationToken)
    {
        logger.LogDebug("Обновление канала: {ChannelName}", channel.Name);

        var fresh = await channelRepository.GetByName(channel.Name, cancellationToken);
        if (fresh is null) return;

        var info = await ParseChannelInfo(channel.Name);
        fresh.Info = info;

        channelRepository.Update(fresh);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<ChannelInfo?> ParseChannelInfo(string name)
    {
        const string baseXpath = "//div[contains(@class, \"channel-info-content\")]";

        var channelUrl = $"{ApplicationConstants.TwitchUrl}{name}";
        var response = await camoufox.GetPageHtml(new CamoufoxRequest(channelUrl, 30));

        if (response is null)
        {
            logger.LogWarning("response is null: {ChannelUrl}", channelUrl);
            return null;
        }

        if (string.IsNullOrEmpty(response))
        {
            logger.LogWarning("Html is null: {ChannelUrl}", channelUrl);
            return null;
        }

        var parse = response.GetParse();
        if (parse is null)
        {
            logger.LogWarning("Parse is null: {ChannelUrl}", channelUrl);
            return null;
        }

        var avatarUrl = parse.GetAttributeValue($"{baseXpath}//img", "src")?.Trim('"', '\\');
        var channelTitle = parse.GetInnerText($"{baseXpath}//h1");
        var viewers = parse.GetInnerText($"{baseXpath}//strong[contains(@data-a-target, \"animated-channel-viewers-count\")]");
        var description = parse.GetInnerText($"{baseXpath}//p[contains(@data-a-target, \"stream-title\")]");
        var category = parse.GetInnerText($"{baseXpath}//a[contains(@data-a-target, \"stream-game-link\")]");

        if (string.IsNullOrEmpty(avatarUrl))
        {
            logger.LogWarning("AvatarUrl is null: {ChannelName}", name);
            return null;
        }

        if (string.IsNullOrEmpty(channelTitle))
        {
            logger.LogWarning("ChannelTitle is null: {ChannelName}", name);
            return null;
        }

        if (string.IsNullOrEmpty(viewers))
        {
            // logger.LogWarning("Viewers is null: {ChannelName}", name);
            return null;
        }

        if (string.IsNullOrEmpty(description))
        {
            logger.LogWarning("Description is null: {ChannelName}", name);
            return null;
        }

        if (string.IsNullOrEmpty(category))
        {
            logger.LogWarning("Category is null: {ChannelName}", name);
            return null;
        }

        return new ChannelInfo()
        {
            Thumb = avatarUrl,
            Title = channelTitle,
            Viewers = viewers,
            Description = description,
            Category = category
        };
    }
}

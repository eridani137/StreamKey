using Microsoft.Extensions.Logging;
using ParserExtension;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Results;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ChannelService(
    IChannelRepository channelRepository,
    ICamoufoxService camoufox,
    ILogger<ChannelService> logger) : IChannelService
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
        
        await UpdateChannelInfo(channel);

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

    public async Task UpdateChannelInfo(ChannelEntity channel)
    {
        logger.LogInformation("Обновление канала: {ChannelName}", channel.Name);

        var fresh = await channelRepository.GetByName(channel.Name);
        if (fresh is null) return;

        var info = await ParseChannelInfo(channel.Name);
        fresh.Info = info;

        await channelRepository.Update(fresh);
    }

    private async Task<ChannelInfo?> ParseChannelInfo(string name)
    {
        const string baseXpath = "//div[@class='channel-info-content']";

        var channelUrl = $"{ApplicationConstants.TwitchUrl}/{name}";
        var response = await camoufox.GetPageHtml(new CamoufoxRequest(channelUrl, 60));

        if (response is null)
        {
            logger.LogWarning("response is null: {ChannelUrl}", channelUrl);
            return null;
        }

        if (string.IsNullOrEmpty(response.Html))
        {
            logger.LogWarning("Html is null: {ChannelUrl}", channelUrl);
            return null;
        }

        var parse = response.Html.GetParse();
        if (parse is null)
        {
            logger.LogWarning("Parse is null: {ChannelUrl}", channelUrl);
            return null;
        }

        var avatarUrl = parse.GetAttributeValue($"{baseXpath}//img[contains(@class,'tw-image-avatar')]", "src");
        var channelTitle = parse.GetInnerText($"{baseXpath}//h1[contains(@class,'tw-title')]");
        var viewers = parse.GetInnerText($"{baseXpath}//strong[@data-a-target='animated-channel-viewers-count']");
        var description = parse.GetInnerText($"{baseXpath}//p[@data-a-target='stream-title']");
        var category = parse.GetInnerText($"{baseXpath}//a[@data-a-target='stream-game-link']");

        if (string.IsNullOrEmpty(avatarUrl) ||
            string.IsNullOrEmpty(channelTitle) ||
            string.IsNullOrEmpty(viewers) ||
            string.IsNullOrEmpty(description) ||
            string.IsNullOrEmpty(category))
        {
            logger.LogWarning(
                "{AvatarUrl} or {ChannelTitle} or {Viewers} or {Description} or {Category} is null or empty",
                avatarUrl, channelTitle, viewers, description, category);
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
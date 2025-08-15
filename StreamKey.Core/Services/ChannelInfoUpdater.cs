using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParserExtension;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class ChannelInfoUpdater(
    ILogger<ChannelInfoUpdater> logger,
    IServiceProvider serviceProvider,
    ICamoufoxService camoufox)
    : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();

                var channels = await channelRepository.GetAll();
                foreach (var channel in channels)
                {
                    var info = await ParseChannelInfo(channel.Name);
                    if (info is null) continue;
                    
                    channel.Info = info;
                    await channelRepository.Update(channel);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обновления информации о канале");
            }
            finally
            {
                await Task.Delay(UpdateInterval, stoppingToken);
            }
        }
    }

    private async Task<ChannelInfo?> ParseChannelInfo(string name)
    {
        const string baseXpath = "//div[@class='channel-info-content']";
        
        var channelUrl = $"{ApplicationConstants.TwitchUrl}/{name}";
        var response = await camoufox.GetPageHtml(new CamoufoxRequest(channelUrl, 30));

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
        var viewers = parse.GetInnerText($"{baseXpath}//strong[@data-a-target='animated-channel-viewers-count']/span");

        if (string.IsNullOrEmpty(avatarUrl) || string.IsNullOrEmpty(channelTitle) || string.IsNullOrEmpty(viewers))
        {
            logger.LogWarning("{AvatarUrl} or {ChannelTitle} or {Viewers} is null or empty", avatarUrl, channelTitle, viewers);
            return null;
        }

        return new ChannelInfo()
        {
            Thumb = avatarUrl,
            Title = channelTitle,
            Viewers = viewers,
        };
    }
}
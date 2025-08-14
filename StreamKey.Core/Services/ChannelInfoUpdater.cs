using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParserExtension;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;

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

        const string baseXpath = "//div[@class='channel-info-content']";
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                
                var channels = await channelRepository.GetAll();
                foreach (var channel in channels)
                {
                    var channelUrl = $"{ApplicationConstants.TwitchUrl}/{channel.Name}";
                    var response = await camoufox.GetPageHtml(new CamoufoxRequest(channelUrl, 30));

                    if (string.IsNullOrEmpty(response.Html))
                    {
                        logger.LogWarning("Html is null: {ChannelUrl}", channelUrl);
                        continue;
                    }

                    var parse = response.Html.GetParse();
                    if (parse is null)
                    {
                        logger.LogWarning("Parse is null: {ChannelUrl}", channelUrl);
                        continue;
                    }

                    var avatarUrl = parse.GetAttributeValue($"{baseXpath}//img[contains(@class,'tw-image-avatar')]", "src");
                    var channelTitle = parse.GetInnerText($"{baseXpath}//h1[contains(@class,'tw-title')]");
                    var viewers = parse.GetInnerText($"{baseXpath}//strong[@data-a-target='animated-channel-viewers-count']/span");
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
}
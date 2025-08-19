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
    IChannelService channelService)
    : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(5);

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
                    try
                    {
                        await channelService.UpdateChannelInfo(channel);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Ошибка при обновлении информации канала {@Channel}", channel);
                    }
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
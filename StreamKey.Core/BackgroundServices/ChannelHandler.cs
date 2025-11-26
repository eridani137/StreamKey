using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Core.BackgroundServices;

public class ChannelHandler(
    ILogger<ChannelHandler> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

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
                
                await Task.Delay(UpdateInterval, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обновления информации о канале");
            }
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Common;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Core.BackgroundServices;

public class ChannelsBackground(ILogger<ChannelsBackground> logger, IServiceScopeFactory scopeFactory)
    : BackgroundService
{
    private readonly PeriodicTaskRunner<ChannelsBackground> _taskRunner = new(logger);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _taskRunner.RunAsync(TimeSpan.FromMinutes(3), UpdateAllChannels, stoppingToken);
    }

    private async Task UpdateAllChannels(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var channelRepository = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var channels = await channelRepository.GetAll(cancellationToken);

        foreach (var channel in channels)
        {
            try
            {
                await channelService.UpdateChannelInfo(channel, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка обновления информации о канале");
            }
        }
    }
}
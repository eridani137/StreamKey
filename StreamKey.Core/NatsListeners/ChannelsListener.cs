using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.NatsListeners;

public class ChannelsListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    MessagePackNatsSerializer<List<ChannelDto>?> responseSerializer,
    INatsRequestReplyProcessor<string?, List<ChannelDto>?> processor
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync<string?>(
            NatsKeys.GetChannels,
            cancellationToken: stoppingToken
        );

        await processor.ProcessAsync(
            subscription,
            _ => GetChannelsAsync(stoppingToken),
            nats,
            responseSerializer,
            stoppingToken
        );
    }

    private async Task<List<ChannelDto>?> GetChannelsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var channelService = scope.ServiceProvider.GetRequiredService<IChannelService>();

        var channels = await channelService.GetChannels(cancellationToken);
        return channels.Map();
    }
}
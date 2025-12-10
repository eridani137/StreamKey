using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.BackgroundServices;

public class ChannelListener(
    INatsConnection nats,
    INatsSubscriptionProcessor<ClickChannelRequest> processor,
    MessagePackNatsSerializer<ClickChannelRequest> clickChannelRequestSerializer,
    StatisticService statisticService) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return processor.ProcessAsync(
            nats.SubscribeAsync(NatsKeys.ClickChannel, serializer: clickChannelRequestSerializer,
                cancellationToken: stoppingToken),
            HandleClickChannel, stoppingToken);
    }

    private Task HandleClickChannel(ClickChannelRequest dto)
    {
        statisticService.ChannelActivityQueue.Enqueue(dto.Map());

        return Task.CompletedTask;
    }
}
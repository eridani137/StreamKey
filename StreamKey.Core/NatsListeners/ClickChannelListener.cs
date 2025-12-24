using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.NatsListeners;

public class ClickChannelListener(
    INatsConnection nats,
    INatsSubscriptionProcessor<ClickChannelRequest> processor,
    JsonNatsSerializer<ClickChannelRequest> clickChannelRequestSerializer,
    StatisticService service) : BackgroundService
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
        service.ChannelActivityQueue.Enqueue(dto.Map());

        return Task.CompletedTask;
    }
}
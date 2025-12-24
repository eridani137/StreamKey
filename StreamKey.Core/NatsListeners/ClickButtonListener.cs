using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.NatsListeners;

public class ClickButtonListener(
    INatsConnection nats,
    INatsSubscriptionProcessor<ClickButtonRequest> processor,
    JsonNatsSerializer<ClickButtonRequest> clickButtonRequestSerializer,
    StatisticService service) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return processor.ProcessAsync(
            nats.SubscribeAsync(NatsKeys.ClickButton, serializer: clickButtonRequestSerializer,
                cancellationToken: stoppingToken),
            HandleClickButton, stoppingToken);
    }

    private Task HandleClickButton(ClickButtonRequest dto)
    {
        service.ButtonActivityQueue.Enqueue(dto.Map());

        return Task.CompletedTask;
    }
}
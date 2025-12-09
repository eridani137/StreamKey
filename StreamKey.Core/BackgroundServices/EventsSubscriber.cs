using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.BackgroundServices;

public class EventsSubscriber(
    IConnectionMultiplexer mux,
    StatisticService statistics,
    ILogger<ChannelHandler> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = mux.GetSubscriber();

        sub.Subscribe(RedisChannel.Literal(nameof(ClickChannel)), (channel, payload) =>
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ClickChannel>(payload.ToString());
                if (dto is null) return;
                statistics.ChannelActivityQueue.Enqueue(dto.Map());
                
                logger.LogInformation("Событие обработано {EventName}", channel);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка в обработке события {EventName}:{Payload}", channel, payload);
            }
        });
        
        return Task.CompletedTask;
    }
}
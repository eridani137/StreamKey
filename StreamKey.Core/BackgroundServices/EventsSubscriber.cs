using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Events;

namespace StreamKey.Core.BackgroundServices;

public class EventsSubscriber(
    IConnectionMultiplexer mux,
    StatisticService statistics,
    ITelegramUserRepository telegramUserRepository,
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
                var request = JsonSerializer.Deserialize<ClickChannel>(payload.ToString())!;
                statistics.ChannelActivityQueue.Enqueue(request.Map());
                
                logger.LogInformation("Событие обработано {EventName}", channel);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка в обработке события {EventName}:{Payload}", channel, payload);
            }
        });

        sub.SubscribeAsync(RedisChannel.Literal(nameof(TelegramUserRequest)), async void (channel, payload) =>
        {
            try
            {
                var request = JsonSerializer.Deserialize<GetTelegramUserRequest>(payload.ToString())!;
                var user = await telegramUserRepository.GetByTelegramIdNotTracked(request.UserId, stoppingToken);
                
                TelegramUserDto? dto = null;

                if (user != null && user.Hash == request.UserHash)
                {
                    dto = user.MapUserDto();
                }
                
                await SendTelegramUserResponse(request.RequestId, request.ConnectionId, dto);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка в обработке события {EventName}:{Payload}", channel, payload);
            }
        });
        
        return Task.CompletedTask;
    }

    private Task SendTelegramUserResponse(Guid id, string connectionId, TelegramUserDto? dto)
    {
        var sub = mux.GetSubscriber();
        
        var response = new GetTelegramUserResponse
        {
            RequestId = id,
            ConnectionId = connectionId,
            User = dto
        };

        return sub.PublishAsync(
            RedisChannel.Literal($"{nameof(GetTelegramUserResponse)}:{id}"),
            JsonSerializer.Serialize(response));
    }
}
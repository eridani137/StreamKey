using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Stores;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.BackgroundServices;

public class ConnectionListener(INatsConnection nats, ILogger<ConnectionListener> logger) : BackgroundService
{
    private readonly MessagePackNatsSerializer<UserSessionMessage> _userSessionSerializer = new();
    private readonly MessagePackNatsSerializer<ClickChannelRequest> _clickChannelSerializer = new();
    
    private static readonly TimeSpan MinimumSessionTime = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var userSessionHandlers = new Dictionary<string, Func<UserSessionMessage, Task>>
        {
            [NatsKeys.Connection] = msg =>
            {
                if (msg.Session is not null) ConnectionRegistry.AddConnection(msg.ConnectionId, msg.Session);
                return Task.CompletedTask;
            },
            [NatsKeys.Disconnection] = msg =>
            {
                ConnectionRegistry.MoveToDisconnected(msg.ConnectionId);
                return Task.CompletedTask;
            },
            [NatsKeys.UpdateActivity] = msg =>
            {
                if (msg.Session is not null)
                    ConnectionRegistry.UpdateActivity(msg.ConnectionId, msg.Session, MinimumSessionTime);
                return Task.CompletedTask;
            }
        };

        var userSessionTasks = userSessionHandlers.Select(kv =>
        {
            var subscription = nats.SubscribeAsync(kv.Key, serializer: _userSessionSerializer, cancellationToken: stoppingToken);
            return ProcessSubscription(subscription, kv.Value, stoppingToken);
        });

        var clickChannelSubscription = nats.SubscribeAsync(NatsKeys.ClickChannel, serializer: _clickChannelSerializer, cancellationToken: stoppingToken);
        var clickChannelTask = ProcessSubscription(clickChannelSubscription, HandleClickChannel, stoppingToken);

        await Task.WhenAll(userSessionTasks.Append(clickChannelTask));
    }

    private async Task ProcessSubscription<T>(
        IAsyncEnumerable<NatsMsg<T>> subscription,
        Func<T, Task> handle,
        CancellationToken token)
    {
        await foreach (var msg in subscription.WithCancellation(token))
        {
            try
            {
                if (msg.Data is not null)
                {
                    await handle(msg.Data);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при обработке NATS-сообщения: {Subject}", msg.Subject);
            }
        }
    }
    
    private Task HandleClickChannel(ClickChannelRequest dto)
    {
        logger.LogInformation("ClickChannel received: {ChannelId}", dto.ChannelName);
        return Task.CompletedTask;
    }
}
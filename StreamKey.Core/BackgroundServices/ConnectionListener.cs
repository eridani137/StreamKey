using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Stores;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.BackgroundServices;

public class ConnectionListener(
    INatsConnection nats,
    INatsSubscriptionProcessor<UserSessionMessage> processor,
    MessagePackNatsSerializer<UserSessionMessage> userSessionMessageSerializer) : BackgroundService
{
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

        var userSessionTasks = userSessionHandlers.Select(kvp => processor.ProcessAsync(
            nats.SubscribeAsync(kvp.Key, serializer: userSessionMessageSerializer, cancellationToken: stoppingToken),
            kvp.Value, stoppingToken));

        await Task.WhenAll(userSessionTasks);
    }
}
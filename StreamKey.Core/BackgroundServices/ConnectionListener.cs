using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using Serilog;
using StreamKey.Core.Stores;
using StreamKey.Shared;
using StreamKey.Shared.Types;

namespace StreamKey.Core.BackgroundServices;

public class ConnectionListener(NatsConnection nats, ILogger<ConnectionListener> logger) : BackgroundService
{
    private readonly MessagePackNatsSerializer<UserSessionMessage> _serializer = new();
    private static readonly TimeSpan MinimumSessionTime = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var addSub = nats.SubscribeAsync<UserSessionMessage>(
            "connections.add",
            serializer: _serializer,
            cancellationToken: stoppingToken);

        var removeSub = nats.SubscribeAsync<UserSessionMessage>(
            "connections.remove",
            serializer: _serializer,
            cancellationToken: stoppingToken);

        var activitySub = nats.SubscribeAsync<UserSessionMessage>(
            "connections.activity",
            serializer: _serializer,
            cancellationToken: stoppingToken);

        await Task.WhenAll(
            ProcessSubscription(addSub, (msg) =>
            {
                if (msg.Data?.Session is not null)
                {
                    ConnectionRegistry.AddConnection(msg.Data.ConnectionId, msg.Data.Session);
                }
            }, stoppingToken),

            ProcessSubscription(removeSub, (msg) =>
            {
                if (msg.Data is not null)
                {
                    ConnectionRegistry.MoveToDisconnected(msg.Data.ConnectionId);
                }
            }, stoppingToken),

            ProcessSubscription(activitySub, (msg) =>
            {
                if (msg.Data?.Session is not null)
                {
                    ConnectionRegistry.UpdateActivity(msg.Data.ConnectionId, msg.Data.Session, MinimumSessionTime);
                }
            }, stoppingToken)
        );
    }

    private async Task ProcessSubscription(
        IAsyncEnumerable<NatsMsg<UserSessionMessage>> subscription,
        Action<NatsMsg<UserSessionMessage>> handle,
        CancellationToken token)
    {
        await foreach (var msg in subscription.WithCancellation(token))
        {
            try
            {
                handle(msg);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при обработке NATS-сообщения: {@Message}", msg);
            }
        }
    }
}
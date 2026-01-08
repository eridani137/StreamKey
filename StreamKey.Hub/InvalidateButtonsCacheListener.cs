using Microsoft.Extensions.Caching.Memory;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Hubs;

namespace StreamKey.Hub;

public class InvalidateButtonsCacheListener(
    INatsConnection nats,
    INatsSubscriptionProcessor<int> processor,
    JsonNatsSerializer<int> serializer,
    IMemoryCache cache,
    ILogger<InvalidateButtonsCacheListener> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return processor.ProcessAsync(
            nats.SubscribeAsync(NatsKeys.InvalidateButtonsCache, serializer: serializer,
                cancellationToken: stoppingToken),
            InvalidateButtonsCacheAsync, stoppingToken);
    }

    private Task InvalidateButtonsCacheAsync(int key)
    {
        var position = (ButtonPosition)key;

        if (!Enum.IsDefined(position))
        {
            logger.LogWarning("Invalid ButtonPosition value: {Key}", key);
            return Task.CompletedTask;
        }
        
        cache.Remove(BrowserExtensionHub.GetButtonsCacheKey(position));

        return Task.CompletedTask;
    }
}
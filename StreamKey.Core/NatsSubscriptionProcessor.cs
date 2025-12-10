using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;

namespace StreamKey.Core;

public sealed class NatsSubscriptionProcessor<T>(ILogger<NatsSubscriptionProcessor<T>> logger)
    : INatsSubscriptionProcessor<T>
{
    public async Task ProcessAsync(
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
                logger.LogError(e,
                    "Ошибка при обработке NATS-сообщения: {Subject}",
                    msg.Subject);
            }
        }
    }
}
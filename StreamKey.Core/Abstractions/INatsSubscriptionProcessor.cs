using NATS.Client.Core;

namespace StreamKey.Core.Abstractions;

public interface INatsSubscriptionProcessor<T>
{
    Task ProcessAsync(
        IAsyncEnumerable<NatsMsg<T>> subscription,
        Func<T, Task> handle,
        CancellationToken token);
}
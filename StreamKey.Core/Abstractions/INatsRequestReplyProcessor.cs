using NATS.Client.Core;

namespace StreamKey.Core.Abstractions;

public interface INatsRequestReplyProcessor<TRequest, TResponse>
{
    Task ProcessAsync(
        IAsyncEnumerable<NatsMsg<TRequest>> subscription,
        Func<TRequest, Task<TResponse>> handle,
        INatsConnection nats,
        INatsSerialize<TResponse?>? responseSerializer,
        CancellationToken token);
}
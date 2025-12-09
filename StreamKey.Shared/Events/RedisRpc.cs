using System.Text.Json;
using StackExchange.Redis;
using StreamKey.Shared.Abstractions;

namespace StreamKey.Shared.Events;

public class RedisRpc(IConnectionMultiplexer mux) : IRedisRpc
{
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task<TResponse?> CallAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(7);

        var requestId = Guid.CreateVersion7();
        
        var responseChannel = new RedisChannel($"{method}:response:{requestId}", RedisChannel.PatternMode.Literal);
        var requestChannel = new RedisChannel($"{method}:request", RedisChannel.PatternMode.Literal);

        var tcs = new TaskCompletionSource<TResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var sub = mux.GetSubscriber();

        await sub.SubscribeAsync(responseChannel, (_, payload) =>
        {
            try
            {
                var response = JsonSerializer.Deserialize<TResponse>(payload.ToString())!;
                tcs.TrySetResult(response);
            }
            catch
            {
                tcs.TrySetResult(default);
            }
        });

        var envelope = new RpcRequestEnvelope
        {
            RequestId = requestId,
            Payload = JsonSerializer.Serialize(request),
        };

        await sub.PublishAsync(requestChannel, JsonSerializer.Serialize(envelope));

        var task = tcs.Task;

        var completed = await Task.WhenAny(task, Task.Delay(timeout.Value, cancellationToken));

        await sub.UnsubscribeAsync(responseChannel);

        return completed == task 
            ? task.Result 
            : default;
    }
}
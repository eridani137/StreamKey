using System.Text.Json;
using StackExchange.Redis;

namespace StreamKey.Shared.Events;

public class RedisPublisher(IConnectionMultiplexer mux)
{
    public Task PublishAsync<T>(RedisChannel channel, T message)
    {
        var json = JsonSerializer.Serialize(message);
        return mux.GetSubscriber().PublishAsync(channel, json);
    }
}
using System.Text.Json;
using StackExchange.Redis;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;

namespace StreamKey.Shared.Stores;

public class RedisStatisticStore(IConnectionMultiplexer mux) : IStatisticStore
{
    private readonly IDatabase _db = mux.GetDatabase();
    
    public async Task SaveClickAsync(ClickChannel click)
    {
        var json = JsonSerializer.Serialize(click);

        await _db.ListRightPushAsync(RedisStatisticKeys.ChannelClickList, json);
    }
}
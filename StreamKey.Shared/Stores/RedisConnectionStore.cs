using System.Text.Json;
using StackExchange.Redis;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.Types;

namespace StreamKey.Shared.Stores;

public class RedisConnectionStore(IConnectionMultiplexer mux) : IConnectionStore
{
    private readonly IDatabase _db = mux.GetDatabase();
    
    private const string ActiveKey = "signalr:active";
    private const string DisconnectedKey = "signalr:disconnected";
    private const string SessionIndexKey = "signalr:session";

    public async Task AddConnectionAsync(string connectionId, UserSession session)
    {
        var json = JsonSerializer.Serialize(session);

        await _db.StringSetAsync($"{ActiveKey}:{connectionId}", json);
        await _db.StringSetAsync($"{SessionIndexKey}:{session.SessionId}", connectionId);
    }

    public async Task<UserSession?> GetSessionAsync(string connectionId)
    {
        var json = await _db.StringGetAsync($"{ActiveKey}:{connectionId}");
        if (!json.HasValue) return null;

        return JsonSerializer.Deserialize<UserSession>(json.ToString());
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        await _db.KeyDeleteAsync($"{ActiveKey}:{connectionId}");
    }

    public async Task MoveToDisconnectedAsync(string connectionId, UserSession session)
    {
        var json = JsonSerializer.Serialize(session);

        await _db.StringSetAsync($"{DisconnectedKey}:{connectionId}", json);
        await _db.KeyDeleteAsync($"{ActiveKey}:{connectionId}");
    }

    public async Task<UserSession?> GetDisconnectedAsync(string connectionId)
    {
        var json = await _db.StringGetAsync($"{DisconnectedKey}:{connectionId}");
        if (!json.HasValue) return null;

        return JsonSerializer.Deserialize<UserSession>(json.ToString());
    }

    public async Task<string?> GetConnectionIdBySessionId(Guid sessionId)
    {
        var value = await _db.StringGetAsync($"{SessionIndexKey}:{sessionId}");
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<Dictionary<string, UserSession>> GetAllActiveConnectionsAsync()
    {
        var result = new Dictionary<string, UserSession>();

        var server = mux.GetServer(mux.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{ActiveKey}:*").ToArray();

        if (keys.Length == 0) return result;

        var values = await _db.StringGetAsync(keys.Select(k => k).ToArray());

        for (var i = 0; i < keys.Length; i++)
        {
            if (!values[i].HasValue) continue;

            var key = keys[i].ToString();
            var json = values[i];
            
            var session = JsonSerializer.Deserialize<UserSession>(json.ToString());
            if (session == null) continue;

            var connectionId = key[(ActiveKey.Length + 1)..];
            result[connectionId] = session;
        }

        return result;
    }
}
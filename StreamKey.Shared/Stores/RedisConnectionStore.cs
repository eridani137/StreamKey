using System.Text.Json;
using StackExchange.Redis;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.Types;

namespace StreamKey.Shared.Stores;

public class RedisConnectionStore(IConnectionMultiplexer mux) : IConnectionStore
{
    private readonly IDatabase _db = mux.GetDatabase();

    public async Task AddConnectionAsync(string connectionId, UserSession session)
    {
        var json = JsonSerializer.Serialize(session);

        await _db.StringSetAsync(RedisKeys.ActiveConnection(connectionId), json);
        await _db.SetAddAsync(RedisKeys.ActiveConnectionsSet, connectionId);
        await _db.StringSetAsync(RedisKeys.SessionIndex(session.SessionId), connectionId);
    }

    public async Task<UserSession?> GetSessionAsync(string connectionId)
    {
        var json = await _db.StringGetAsync(RedisKeys.ActiveConnection(connectionId));
        return json.HasValue
            ? JsonSerializer.Deserialize<UserSession>(json.ToString())
            : null;
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        await _db.KeyDeleteAsync(RedisKeys.ActiveConnection(connectionId));
        await _db.SetRemoveAsync(RedisKeys.ActiveConnectionsSet, connectionId);
    }

    public async Task MoveToDisconnectedAsync(string connectionId, UserSession session)
    {
        var json = JsonSerializer.Serialize(session);

        await _db.StringSetAsync(RedisKeys.DisconnectedConnection(connectionId), json);

        await _db.KeyDeleteAsync(RedisKeys.ActiveConnection(connectionId));
        await _db.SetRemoveAsync(RedisKeys.ActiveConnectionsSet, connectionId);
    }

    public async Task<UserSession?> GetDisconnectedAsync(string connectionId)
    {
        var json = await _db.StringGetAsync(RedisKeys.DisconnectedConnection(connectionId));
        return json.HasValue
            ? JsonSerializer.Deserialize<UserSession>(json.ToString())
            : null;
    }

    public async Task<string?> GetConnectionIdBySessionId(Guid sessionId)
    {
        var value = await _db.StringGetAsync(RedisKeys.SessionIndex(sessionId));
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<Dictionary<string, UserSession>> GetAllActiveConnectionsAsync()
    {
        var result = new Dictionary<string, UserSession>();

        var connectionIds = await _db.SetMembersAsync(RedisKeys.ActiveConnectionsSet);
        if (connectionIds.Length == 0) return result;

        var keys = connectionIds
            .Select(id => (RedisKey)RedisKeys.ActiveConnection(id!))
            .ToArray();

        var values = await _db.StringGetAsync(keys);

        for (var i = 0; i < connectionIds.Length; i++)
        {
            if (!values[i].HasValue) continue;

            var session = JsonSerializer.Deserialize<UserSession>(values[i].ToString());
            if (session == null) continue;

            result[connectionIds[i]!] = session;
        }

        return result;
    }

    public async Task<Dictionary<string, UserSession>> GetAllDisconnectedConnectionsAsync()
    {
        var result = new Dictionary<string, UserSession>();

        var server = mux.GetServer(mux.GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"{RedisKeys.DisconnectedConnectionPrefix}:*").ToArray();

        if (keys.Length == 0) return result;

        var values = await _db.StringGetAsync(keys);

        for (var i = 0; i < keys.Length; i++)
        {
            if (!values[i].HasValue) continue;

            var session = JsonSerializer.Deserialize<UserSession>(values[i].ToString());
            if (session == null) continue;

            var connectionId = keys[i].ToString()!.Split(':').Last();
            result[connectionId] = session;
        }

        return result;
    }
}
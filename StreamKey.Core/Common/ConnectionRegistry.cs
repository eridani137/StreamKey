using System.Collections.Concurrent;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.Common;

public static class ConnectionRegistry
{
    public static readonly ConcurrentDictionary<string, UserSession> ActiveConnections = new();
    public static readonly ConcurrentDictionary<string, UserSession> DisconnectedConnections = new();
    public static readonly ConcurrentQueue<TelegramAuthDtoWithSessionId> NewTelegramUsers = new();

    public static string? GetConnectionIdBySessionId(Guid sessionId)
    {
        return (from kvp in ActiveConnections where kvp.Value.SessionId == sessionId select kvp.Key).FirstOrDefault();
    }

    public static bool AddConnection(string connectionId, UserSession session)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(session);

        return ActiveConnections.TryAdd(connectionId, session);
    }

    public static bool RemoveConnection(string connectionId)
    {
        var removed = ActiveConnections.TryRemove(connectionId, out _);
        DisconnectedConnections.TryRemove(connectionId, out _);
        return removed;
    }

    public static bool MoveToDisconnected(string connectionId)
    {
        if (!ActiveConnections.TryRemove(connectionId, out var session)) return false;

        return DisconnectedConnections.TryAdd(connectionId, session);
    }

    public static void UpdateActivity(string connectionId, UserSession activityUpdate)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(activityUpdate);

        var now = DateTimeOffset.UtcNow;

        ActiveConnections.AddOrUpdate(
            connectionId,
            _ =>
            {
                activityUpdate.StartedAt = now;
                return activityUpdate;
            },
            (_, existingSession) =>
            {
                existingSession.UserId ??= activityUpdate.UserId;

                if (existingSession.UpdatedAt != DateTimeOffset.MinValue)
                {
                    var delta = now - existingSession.UpdatedAt;
                    if (delta.TotalMilliseconds > 0)
                    {
                        existingSession.AccumulatedTime += delta;
                    }
                }

                existingSession.UpdatedAt = now;
                return existingSession;
            }
        );
    }

    public static UserSession? GetConnection(string connectionId)
        => ActiveConnections.TryGetValue(connectionId, out var session) ? session : null;

    public static IEnumerable<UserSession> GetAllActive() => ActiveConnections.Values.ToList();
    public static IEnumerable<UserSession> GetAllDisconnected() => DisconnectedConnections.Values.ToList();
}
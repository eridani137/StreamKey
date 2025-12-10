using System.Collections.Concurrent;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core;

public static class ConnectionRegistry
{
    public static readonly ConcurrentDictionary<string, UserSession> ActiveConnections = new();
    public static readonly ConcurrentDictionary<string, UserSession> DisconnectedConnections = new();

    public static void AddConnection(string connectionId, UserSession session)
        => ActiveConnections[connectionId] = session;

    public static void RemoveConnection(string connectionId)
    {
        ActiveConnections.TryRemove(connectionId, out _);
        DisconnectedConnections.TryRemove(connectionId, out _);
    }

    public static void MoveToDisconnected(string connectionId)
    {
        if (ActiveConnections.TryRemove(connectionId, out var session))
        {
            DisconnectedConnections[connectionId] = session;
        }
    }

    public static void UpdateActivity(string connectionId, UserSession activity, TimeSpan minimumSessionTime)
    {
        var now = DateTimeOffset.UtcNow;
        
        if (!ActiveConnections.TryGetValue(connectionId, out var session))
        {
            activity.StartedAt = now;
            ActiveConnections[connectionId] = activity;
            return;
        }
        
        session.UserId ??= activity.UserId;

        if (session.UpdatedAt == DateTimeOffset.MinValue || session.UpdatedAt >= now.Add(-minimumSessionTime))
        {
            if (session.UpdatedAt == DateTimeOffset.MinValue)
            {
                session.StartedAt = now;
            }

            session.UpdatedAt = now;
            session.AccumulatedTime += minimumSessionTime;

            ActiveConnections[connectionId] = session;
        }
    }

    public static UserSession? GetConnection(string connectionId)
        => ActiveConnections.TryGetValue(connectionId, out var session) ? session : null;

    public static IEnumerable<UserSession> GetAllActive() => ActiveConnections.Values;
    public static IEnumerable<UserSession> GetAllDisconnected() => DisconnectedConnections.Values;
}
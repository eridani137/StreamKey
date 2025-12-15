using System.Collections.Concurrent;
using StreamKey.Core.Common;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class StatisticService
{
    public ConcurrentQueue<ViewStatisticEntity> ViewStatisticQueue { get; } = new();

    public readonly ConcurrentDictionary<string, UserSessionEntity> OnlineUsers = new();
    
    public ConcurrentQueue<ClickChannelEntity> ChannelActivityQueue { get; } = new();
    
    public OnlineResponse GetOnline()
    {
        var active = ConnectionRegistry
            .GetAllActive()
            .Count(s => s.UserId is not null);
        var sleeping = ConnectionRegistry.ActiveConnections.Count - active;

        return new OnlineResponse()
        {
            Total = OnlineUsers.Count + ConnectionRegistry.ActiveConnections.Count,
            ConnectionsCount = ConnectionRegistry.ActiveConnections.Count,
            OldVersions = OnlineUsers.Count,
            Active = active,
            Sleeping = sleeping
        };
    }

    public void UpdateUserActivity(UpdateUserActivityRequest updateUserActivityRequest)
    {
        var currentTime = DateTimeOffset.UtcNow;
    
        var session = OnlineUsers.GetOrAdd(
            updateUserActivityRequest.UserId, 
            _ => new UserSessionEntity 
            { 
                StartedAt = currentTime,
                UpdatedAt = currentTime,
                UserId = updateUserActivityRequest.UserId,
                SessionId  = updateUserActivityRequest.SessionId,
            }
        );
        
        var elapsedSinceLastUpdate = currentTime - session.UpdatedAt;
        session.AccumulatedTime += elapsedSinceLastUpdate;
        
        session.UpdatedAt = currentTime;
    }
}
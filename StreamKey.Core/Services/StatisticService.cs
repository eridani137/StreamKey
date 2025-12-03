using System.Collections.Concurrent;
using StreamKey.Core.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class StatisticService
{
    public ConcurrentQueue<ViewStatisticEntity> ViewStatisticQueue { get; } = new();

    // public readonly ConcurrentDictionary<string, UserSessionEntity> OnlineUsers = new();
    
    public ConcurrentQueue<ClickChannelEntity> ChannelActivityQueue { get; } = new();

    // public void UpdateUserActivity(ActivityRequest activityRequest)
    // {
    //     var currentTime = DateTimeOffset.UtcNow;
    //
    //     var session = OnlineUsers.GetOrAdd(
    //         activityRequest.UserId, 
    //         _ => new UserSessionEntity 
    //         { 
    //             StartedAt = currentTime,
    //             UpdatedAt = currentTime,
    //             UserId = activityRequest.UserId,
    //             SessionId  = activityRequest.SessionId,
    //         }
    //     );
    //     
    //     var elapsedSinceLastUpdate = currentTime - session.UpdatedAt;
    //     session.AccumulatedTime += elapsedSinceLastUpdate;
    //     
    //     session.UpdatedAt = currentTime;
    // }
}
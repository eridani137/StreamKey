using System.Collections.Concurrent;
using StreamKey.Core.DTOs;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Core.Services;

public class StatisticService
{
    public ConcurrentQueue<ViewStatisticEntity> ViewStatisticQueue { get; } = new();

    public readonly ConcurrentDictionary<string, UserSession> OnlineUsers = new();

    public void UpdateUserActivity(ActivityRequest activityRequest)
    {
        var currentTime = DateTimeOffset.UtcNow;
    
        var session = OnlineUsers.GetOrAdd(
            activityRequest.UserId, 
            _ => new UserSession { UpdatedAt = currentTime }
        );
    
        session.UpdatedAt = currentTime;
    }
}
using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Infrastructure.Repositories;

public class UserSessionRepository(ApplicationDbContext context)
    : BaseRepository<UserSessionEntity>(context)
{
    public async Task<UserTimeSpentStats> GetAverageTimeSpent(int hours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
    
        var sessions = await GetSet()
            .Where(s => s.UpdatedAt >= cutoffTime)
            .Select(s => new { s.UserId, s.AccumulatedTime })
            .ToListAsync();
    
        if (sessions.Count == 0)
        {
            return new UserTimeSpentStats(TimeSpan.Zero, 0, TimeSpan.Zero);
        }
    
        var userTotalTimes = sessions
            .GroupBy(s => s.UserId)
            .Select(g => g.Sum(s => s.AccumulatedTime.TotalSeconds))
            .ToList();
    
        var averageSeconds = userTotalTimes.Average();
        var sortedTimes = userTotalTimes.OrderBy(t => t).ToList();
        var medianSeconds = sortedTimes.Count % 2 == 0 
            ? (sortedTimes[sortedTimes.Count / 2 - 1] + sortedTimes[sortedTimes.Count / 2]) / 2.0
            : sortedTimes[sortedTimes.Count / 2];
    
        return new UserTimeSpentStats(
            TimeSpan.FromSeconds(averageSeconds), 
            userTotalTimes.Count, 
            TimeSpan.FromSeconds(medianSeconds)
        );
    }
}
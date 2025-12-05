using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Infrastructure.Repositories;

public class UserSessionRepository(ApplicationDbContext context)
    : BaseRepository<UserSessionEntity>(context)
{
    public async Task<UsersPerTimeStatistic> GetUsersPerMonthStatistic(DateOnly date, CancellationToken cancellationToken)
    {
        var firstDayOfMonth = new DateOnly(date.Year, date.Month, 1);
        var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
    
        var startOfMonth = new DateTimeOffset(firstDayOfMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var startOfNextMonth = new DateTimeOffset(firstDayOfNextMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var uniqueUsers = await GetSet()
            .Where(us => us.StartedAt >= startOfMonth && us.StartedAt < startOfNextMonth)
            .Select(us => us.UserId)
            .Distinct()
            .ToListAsync(cancellationToken: cancellationToken);

        return new UsersPerTimeStatistic
        {
            UniqueUsersCount = uniqueUsers.Count
        };
    }
    
    public async Task<UsersPerTimeStatistic> GetUsersPerDayStatistic(DateOnly date, CancellationToken cancellationToken)
    {
        var startOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endOfDay = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var uniqueUsers = await GetSet()
            .Where(us => us.StartedAt.Date >= startOfDay.Date && us.StartedAt.Date <= endOfDay.Date)
            .Select(us => us.UserId)
            .Distinct()
            .ToListAsync(cancellationToken: cancellationToken);

        return new UsersPerTimeStatistic
        {
            UniqueUsersCount = uniqueUsers.Count
        };
    }
    
    public async Task<UserTimeSpentStats> GetAverageTimeSpent(int hours, CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        
        var perUserSeconds = await GetSet()
            .Where(s => s.UpdatedAt >= cutoffTime)
            .GroupBy(s => s.UserId)
            .Select(g => g.Sum(s => s.AccumulatedTime.TotalSeconds))
            .ToListAsync(cancellationToken: cancellationToken);
        
        if (perUserSeconds.Count == 0)
        {
            return new UserTimeSpentStats(TimeSpan.Zero, 0, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
        }
        
        perUserSeconds.Sort();
        
        var count = perUserSeconds.Count;
        var average = perUserSeconds.Average();
        var median = (count % 2 == 0)
            ? (perUserSeconds[count / 2 - 1] + perUserSeconds[count / 2]) / 2.0
            : perUserSeconds[count / 2];
        var min = perUserSeconds[0];
        var max = perUserSeconds[^1];

        return new UserTimeSpentStats(
            TimeSpan.FromSeconds(average),
            count,
            TimeSpan.FromSeconds(median),
            TimeSpan.FromSeconds(min),
            TimeSpan.FromSeconds(max));
    }
}
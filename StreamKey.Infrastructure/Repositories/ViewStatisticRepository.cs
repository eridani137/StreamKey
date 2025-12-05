using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Infrastructure.Repositories;

public class ViewStatisticRepository(ApplicationDbContext context)
    : BaseRepository<ViewStatisticEntity>(context)
{
    public async Task<List<ChannelViewStatistic>> GetTopViewedChannelsAsync(int hours, int count, CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        
        return await GetSet()
            .Where(v => v.DateTime >= cutoffTime)
            .GroupBy(v => v.ChannelName)
            .Select(g => new ChannelViewStatistic
            {
                ChannelName = g.Key,
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
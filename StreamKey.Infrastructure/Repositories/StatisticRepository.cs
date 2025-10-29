using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Infrastructure.Repositories;

public class StatisticRepository(ApplicationDbContext context)
    : BaseRepository<ViewStatisticEntity>(context)
{
    public async Task<List<ChannelViewStatistic>> GetTop10ViewedChannelsAsync()
    {
        return await GetSet()
            .GroupBy(v => new { v.ChannelName })
            .Select(g => new ChannelViewStatistic
            {
                ChannelName = g.Key.ChannelName,
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(10)
            .ToListAsync();
    }
}
using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelClickRepository(ApplicationDbContext context)
    : BaseRepository<ClickChannelEntity>(context)
{
    public async Task<ChannelClicksStatistic> GetChannelClicksCount(string channelName, int hours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);

        var statistic = await GetSet()
            .Where(cc => cc.DateTime >= cutoffTime && cc.ChannelName == channelName)
            .GroupBy(cc => cc.ChannelName)
            .Select(g => new ChannelClicksStatistic
            {
                ChannelName = g.Key,
                ClickCount = g.Count(),
                UniqueUsers = g.Select(x => x.UserId).Distinct().Count()
            })
            .FirstOrDefaultAsync();

        return statistic ?? new ChannelClicksStatistic 
        { 
            ChannelName = channelName, 
            ClickCount = 0, 
            UniqueUsers = 0 
        };
    }
}
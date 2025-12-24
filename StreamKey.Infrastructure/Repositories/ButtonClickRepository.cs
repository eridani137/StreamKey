using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ButtonClickRepository(ApplicationDbContext context)
    : BaseRepository<ClickButtonEntity>(context)
{
    public async Task<ButtonClicksStatistic> GetButtonClicksCount(string link, int hours, CancellationToken cancellationToken)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);

        var statistic = await GetSet()
            .Where(cb => cb.DateTime >= cutoffTime && cb.Link == link)
            .GroupBy(cb => cb.Link)
            .Select(g => new ButtonClicksStatistic()
            {
                Link = g.Key,
                ClickCount = g.Count(),
                UniqueUsers = g.Select(x => x.UserId).Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        return statistic ?? new ButtonClicksStatistic()
        {
            Link = link,
            ClickCount = 0,
            UniqueUsers = 0
        };
    }
}
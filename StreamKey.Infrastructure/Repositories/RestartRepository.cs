using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class RestartRepository(ApplicationDbContext context) : BaseRepository<RestartEntity>(context), IRestartRepository
{
    public async Task<RestartEntity?> GetLastRestart(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        return await GetSet()
            .Where(e => e.DateTime >= today && e.DateTime < tomorrow)
            .OrderByDescending(e => e.DateTime)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }
}
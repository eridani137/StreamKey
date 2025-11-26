using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class RestartRepository(ApplicationDbContext context) : BaseRepository<RestartEntity>(context), IRestartRepository
{
    public async Task<RestartEntity?> GetLastRestart()
    {
        return await GetSet()
            .Where(e => e.DateTime <= DateTime.UtcNow)
            .OrderByDescending(e => e.DateTime)
            .FirstOrDefaultAsync();
    }
}
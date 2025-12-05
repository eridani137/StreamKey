using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class TelegramUserRepository(ApplicationDbContext context)
    : BaseRepository<TelegramUserEntity>(context), ITelegramUserRepository
{
    public async Task<TelegramUserEntity?> GetByTelegramId(long id, CancellationToken cancellationToken)
    {
        return await GetSet().FirstOrDefaultAsync(e => e.TelegramId == id, cancellationToken: cancellationToken);
    }

    public async Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id, CancellationToken cancellationToken)
    {
        return await GetSet().AsNoTracking().FirstOrDefaultAsync(e => e.TelegramId == id, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit, CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddHours(-24);
        
        return await GetSet()
            .Where(e => e.UpdatedAt < cutoffDate)
            .OrderBy(e => e.UpdatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}
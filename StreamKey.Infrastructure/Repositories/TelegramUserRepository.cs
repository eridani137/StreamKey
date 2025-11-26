using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class TelegramUserRepository(ApplicationDbContext context)
    : BaseRepository<TelegramUserEntity>(context), ITelegramUserRepository
{
    public async Task<TelegramUserEntity?> GetByTelegramId(long id)
    {
        return await GetSet().FirstOrDefaultAsync(e => e.TelegramId == id);
    }

    public async Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id)
    {
        return await GetSet().AsNoTracking().FirstOrDefaultAsync(e => e.TelegramId == id);
    }

    public async Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit)
    {
        return await GetSet()
            .OrderBy(e => e.UpdatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
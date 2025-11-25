using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class TelegramUserRepository(ApplicationDbContext context)
    : BaseRepository<TelegramUserEntity>(context), ITelegramUserRepository
{
    public async Task Create(TelegramUserEntity entity)
    {
        await Add(entity);
        await Save();
    }

    Task ITelegramUserRepository.Update(TelegramUserEntity entity)
    {
        Update(entity);
        return Save();
    }

    public async Task<TelegramUserEntity?> GetByTelegramId(long id)
    {
        return await GetSet().FirstOrDefaultAsync(e => e.TelegramId == id);
    }
}
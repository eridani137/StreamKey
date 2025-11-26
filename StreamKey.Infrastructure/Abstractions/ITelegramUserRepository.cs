using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface ITelegramUserRepository : IBaseRepository<TelegramUserEntity>
{
    Task<TelegramUserEntity?> GetByTelegramId(long id);
    
    Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id);

    Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit);
}
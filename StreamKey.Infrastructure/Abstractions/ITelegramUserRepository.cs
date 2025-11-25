using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface ITelegramUserRepository : IBaseRepository<TelegramUserEntity>
{
    Task<TelegramUserEntity?> GetByTelegramId(long id);
}
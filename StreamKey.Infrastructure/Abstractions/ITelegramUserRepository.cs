using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface ITelegramUserRepository
{
    Task<TelegramUserEntity?> GetByTelegramId(long id);
}
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface ITelegramUserRepository
{
    Task Create(TelegramUserEntity entity);
    
    Task Update(TelegramUserEntity entity);
    
    Task<TelegramUserEntity?> GetByTelegramId(long id);
}
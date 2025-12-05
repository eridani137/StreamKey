using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface ITelegramUserRepository : IBaseRepository<TelegramUserEntity>
{
    Task<TelegramUserEntity?> GetByTelegramId(long id, CancellationToken cancellationToken);
    
    Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit, CancellationToken cancellationToken);
}
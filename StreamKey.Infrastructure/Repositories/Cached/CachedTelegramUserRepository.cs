using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedTelegramUserRepository(TelegramUserRepository repository, IMemoryCache cache)
    : BaseCachedRepository<TelegramUserEntity, TelegramUserRepository>(repository, cache), ITelegramUserRepository
{
    protected override string CacheKeyPrefix => "TelegramUser";

    public Task<TelegramUserEntity?> GetByTelegramId(long id)
    {
        return Repository.GetByTelegramId(id);
    }

    public Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id)
    {
        return Repository.GetByTelegramIdNotTracked(id);
    }

    public Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit)
    {
        return Repository.GetOldestUpdatedUsers(limit);
    }
}

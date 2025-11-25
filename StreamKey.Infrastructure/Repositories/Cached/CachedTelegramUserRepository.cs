using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedTelegramUserRepository(TelegramUserRepository repository, IMemoryCache cache)
    : BaseCachedRepository<TelegramUserEntity, TelegramUserRepository>(repository, cache), ITelegramUserRepository
{
    protected override string CacheKeyPrefix => "TelegramUser";

    public async Task Create(TelegramUserEntity entity)
    {
        await Add(entity);
        await Repository.Save();
    }

    async Task ITelegramUserRepository.Update(TelegramUserEntity entity)
    {
        Update(entity);
        await Repository.Save();
    }

    public Task<TelegramUserEntity?> GetByTelegramId(long id)
    {
        throw new NotImplementedException();
    }
}
using Microsoft.EntityFrameworkCore;
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
        return GetCachedData(GetCacheKey(id.ToString()), () => Repository.GetByTelegramIdNotTracked(id));
    }

    public Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit)
    {
        return Repository.GetOldestUpdatedUsers(limit);
    }

    public DbSet<TelegramUserEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(TelegramUserEntity entity)
    {
        InvalidateCache(entity.TelegramId.ToString());
        return Repository.Add(entity);
    }

    public Task AddRange(IEnumerable<TelegramUserEntity> entities)
    {
        InvalidateCache();
        return Repository.AddRange(entities);
    }

    public void Update(TelegramUserEntity entity)
    {
        InvalidateCache(entity.TelegramId.ToString());
        Repository.Update(entity);
    }

    public void Delete(TelegramUserEntity entity)
    {
        InvalidateCache(entity.TelegramId.ToString());
        Repository.Delete(entity);
    }
}

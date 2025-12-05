using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories.Cached;

public class CachedTelegramUserRepository(TelegramUserRepository repository, IMemoryCache cache)
    : BaseCachedRepository<TelegramUserEntity, TelegramUserRepository>(repository, cache), ITelegramUserRepository
{
    protected override string CacheKeyPrefix => "TelegramUser";

    public Task<TelegramUserEntity?> GetByTelegramId(long id, CancellationToken cancellationToken)
    {
        return Repository.GetByTelegramId(id, cancellationToken);
    }

    public Task<TelegramUserEntity?> GetByTelegramIdNotTracked(long id, CancellationToken cancellationToken)
    {
        return GetCachedData(GetCacheKey(id.ToString()), () => Repository.GetByTelegramIdNotTracked(id, cancellationToken));
    }

    public Task<IReadOnlyList<TelegramUserEntity>> GetOldestUpdatedUsers(int limit, CancellationToken cancellationToken)
    {
        return Repository.GetOldestUpdatedUsers(limit, cancellationToken);
    }

    public DbSet<TelegramUserEntity> GetSet()
    {
        return Repository.GetSet();
    }

    public Task Add(TelegramUserEntity entity, CancellationToken cancellationToken)
    {
        InvalidateCache(entity.TelegramId.ToString());
        return Repository.Add(entity, cancellationToken);
    }

    public Task AddRange(IEnumerable<TelegramUserEntity> entities, CancellationToken cancellationToken)
    {
        InvalidateCache();
        return Repository.AddRange(entities, cancellationToken);
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

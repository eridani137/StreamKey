using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class BaseRepository<TEntity>(ApplicationDbContext context)
    : IBaseRepository<TEntity> where TEntity : BaseEntity
{
    public DbSet<TEntity> GetSet()
    {
        return context.Set<TEntity>();
    }
    
    public async Task Add(TEntity entity)
    {
        await GetSet().AddAsync(entity); 
    }

    public void Update(TEntity entity)
    {
        GetSet().Update(entity);
    }

    public void Delete(TEntity entity)
    {
        GetSet().Remove(entity);
    }

    public async Task Save()
    {
        await context.SaveChangesAsync();
    }
}
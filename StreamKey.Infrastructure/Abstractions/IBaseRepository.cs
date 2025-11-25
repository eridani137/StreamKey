using Microsoft.EntityFrameworkCore;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    DbSet<TEntity> GetSet();
    Task Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}

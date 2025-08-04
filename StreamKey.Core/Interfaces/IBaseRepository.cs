using Microsoft.EntityFrameworkCore;
using StreamKey.Application.Entities;

namespace StreamKey.Application.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : BaseEntity
{
    DbSet<TEntity> GetSet();
    Task Add(TEntity entity);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task Save();
}
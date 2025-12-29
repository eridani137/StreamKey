using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IButtonRepository : IBaseRepository<ButtonEntity>
{
    Task<List<ButtonEntity>> GetAll(CancellationToken cancellationToken);
    
    Task<bool> HasEntity(string link, CancellationToken cancellationToken);
    
    Task<ButtonEntity?> GetByLink(string link, CancellationToken cancellationToken);
    Task<ButtonEntity?> GetById(Guid id, CancellationToken cancellationToken);
}
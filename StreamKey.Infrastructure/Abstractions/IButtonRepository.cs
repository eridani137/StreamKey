using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IButtonRepository : IBaseRepository<ButtonEntity>
{
    Task<List<ButtonEntity>> GetByPosition(ButtonPosition position, CancellationToken cancellationToken);
    
    Task<List<ButtonEntity>> GetAll(CancellationToken cancellationToken);
    
    Task<ButtonEntity?> GetById(Guid id, CancellationToken cancellationToken);
}
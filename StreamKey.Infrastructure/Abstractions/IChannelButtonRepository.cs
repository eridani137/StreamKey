using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IChannelButtonRepository : IBaseRepository<ChannelButtonEntity>
{
    Task<List<ChannelButtonEntity>> GetAll(CancellationToken cancellationToken);
    
    Task<bool> HasEntity(string link, CancellationToken cancellationToken);
    
    Task<ChannelButtonEntity?> GetByLink(string link, CancellationToken cancellationToken);
}
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Abstractions;

public interface IRestartRepository : IBaseRepository<RestartEntity>
{
    Task<RestartEntity?> GetLastRestart(CancellationToken cancellationToken);
}
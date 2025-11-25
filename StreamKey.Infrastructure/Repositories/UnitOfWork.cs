using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Infrastructure.Repositories;

public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ButtonRepository(ApplicationDbContext context)
    : BaseRepository<ButtonEntity>(context), IButtonRepository
{
    public async Task<List<ButtonEntity>> GetByPosition(ButtonPosition position, CancellationToken cancellationToken)
    {
        return await GetSet()
            .Where(b => b.Position == position)
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<List<ButtonEntity>> GetAll(CancellationToken cancellationToken)
    {
        return await GetSet()
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<ButtonEntity?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return GetSet().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
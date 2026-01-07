using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ButtonRepository(ApplicationDbContext context)
    : BaseRepository<ButtonEntity>(context), IButtonRepository
{
    public async Task<List<ButtonEntity>> GetAll(CancellationToken cancellationToken)
    {
        return await GetSet().ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<ButtonEntity?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return GetSet().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
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

    public async Task<bool> HasEntity(string link, CancellationToken cancellationToken)
    {
        return await GetSet().AnyAsync(c => c.Link == link, cancellationToken: cancellationToken);
    }

    public Task<ButtonEntity?> GetByLink(string link, CancellationToken cancellationToken)
    {
        return GetSet().FirstOrDefaultAsync(c => c.Link == link, cancellationToken);
    }

    public Task<ButtonEntity?> GetById(Guid id, CancellationToken cancellationToken)
    {
        return GetSet().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
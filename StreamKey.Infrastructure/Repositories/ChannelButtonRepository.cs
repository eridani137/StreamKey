using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelButtonRepository(ApplicationDbContext context)
    : BaseRepository<ChannelButtonEntity>(context), IChannelButtonRepository
{
    public async Task<List<ChannelButtonEntity>> GetAll(CancellationToken cancellationToken)
    {
        return await GetSet().ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> HasEntity(string link, CancellationToken cancellationToken)
    {
        return await GetSet().AnyAsync(c => c.Link == link, cancellationToken: cancellationToken);
    }

    public Task<ChannelButtonEntity?> GetByLink(string link, CancellationToken cancellationToken)
    {
        return GetSet().FirstOrDefaultAsync(c => c.Link == link, cancellationToken);
    }
}
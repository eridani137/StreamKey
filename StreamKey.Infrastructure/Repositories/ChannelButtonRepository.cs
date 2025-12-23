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
}
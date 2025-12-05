using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelRepository(ApplicationDbContext context) 
    : BaseRepository<ChannelEntity>(context), IChannelRepository
{
    public async Task<List<ChannelEntity>> GetAll(CancellationToken cancellationToken)
    {
        return await GetSet().ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> HasEntity(string channelName, CancellationToken cancellationToken)
    {
        return await GetSet().AnyAsync(c => c.Name == channelName, cancellationToken: cancellationToken);
    }

    public async Task<ChannelEntity?> GetByName(string channelName, CancellationToken cancellationToken)
    {
        return await GetSet().FirstOrDefaultAsync(c => c.Name == channelName, cancellationToken: cancellationToken);
    }

    public async Task<ChannelEntity?> GetByPosition(int position, CancellationToken cancellationToken)
    {
        return await GetSet().FirstOrDefaultAsync(c => c.Position == position, cancellationToken: cancellationToken);
    }

    public async Task<bool> HasInPosition(int position, CancellationToken cancellationToken)
    {
        return await GetSet().AnyAsync(c => c.Position == position, cancellationToken: cancellationToken);
    }
}
using Microsoft.EntityFrameworkCore;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelRepository(ApplicationDbContext context) : BaseRepository<ChannelEntity>(context), IChannelRepository
{
    public async Task Create(ChannelEntity channel)
    {
        await Add(channel);
        await Save();
    }

    public async Task Remove(ChannelEntity channel)
    {
        Delete(channel);
        await Save();
    }
    
    public async Task<List<ChannelEntity>> GetAll()
    {
        return await GetSet().ToListAsync();
    }

    public async Task<bool> HasEntity(string channelName)
    {
        return await GetSet().AnyAsync(c => c.Name == channelName);
    }

    public async Task<ChannelEntity?> GetByName(string channelName)
    {
        return await GetSet().FirstOrDefaultAsync(c => c.Name == channelName);
    }

    public async Task<bool> HasInPosition(int position)
    {
        return await GetSet().AnyAsync(c => c.Position == position);
    }
}
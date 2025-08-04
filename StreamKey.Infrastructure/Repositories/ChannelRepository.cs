using StreamKey.Application.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelRepository(ApplicationDbContext context) : BaseRepository<ChannelEntity>(context)
{
    
}
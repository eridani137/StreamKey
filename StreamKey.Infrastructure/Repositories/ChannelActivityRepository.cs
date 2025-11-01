using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class ChannelActivityRepository(ApplicationDbContext context)
    : BaseRepository<ClickChannelEntity>(context)
{
}
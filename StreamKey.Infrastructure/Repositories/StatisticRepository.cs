using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class StatisticRepository(ApplicationDbContext context)
    : BaseRepository<ViewStatisticEntity>(context)
{
}
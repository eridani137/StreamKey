using StreamKey.Shared.Entities;

namespace StreamKey.Infrastructure.Repositories;

public class UserSessionRepository(ApplicationDbContext context)
    : BaseRepository<UserSessionEntity>(context)
{
}
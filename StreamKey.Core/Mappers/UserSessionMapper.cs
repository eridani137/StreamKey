using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

namespace StreamKey.Core.Mappers;

public static class UserSessionMapper
{
    extension(UserSession session)
    {
        public UserSessionEntity Map()
        {
            return new UserSessionEntity()
            {
                UserId = session.UserId ?? string.Empty,
                SessionId = session.SessionId,
                StartedAt = session.StartedAt,
                UpdatedAt = session.UpdatedAt,
                AccumulatedTime = session.AccumulatedTime,
            };
        }
    }
}
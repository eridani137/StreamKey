using StreamKey.Shared.Types;

namespace StreamKey.Shared.Abstractions;

public interface IConnectionStore
{
    Task AddConnectionAsync(string connectionId, UserSession session);
    Task RemoveConnectionAsync(string connectionId);
    Task<UserSession?> GetSessionAsync(string connectionId);

    Task MoveToDisconnectedAsync(string connectionId, UserSession session);
    Task<UserSession?> GetDisconnectedAsync(string connectionId);

    Task<string?> GetConnectionIdBySessionId(Guid sessionId);
}
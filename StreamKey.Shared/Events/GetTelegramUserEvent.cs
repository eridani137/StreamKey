using StreamKey.Shared.DTOs;

namespace StreamKey.Shared.Events;

public class GetTelegramUserRequest
{
    public Guid RequestId { get; init; }
    public long UserId { get; init; }
    public required string UserHash { get; init; }
    public required string ConnectionId { get; init; }
}

public class GetTelegramUserResponse
{
    public Guid RequestId { get; init; }
    public TelegramUserDto? User { get; init; }
    public required string ConnectionId { get; init; }
}
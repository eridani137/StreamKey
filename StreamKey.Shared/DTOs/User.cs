using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record UserSessionMessage
{
    public string ConnectionId { get; init; } = string.Empty;

    public UserSession? Session { get; init; }
}

[MessagePackObject]
public record UserSession
{
    public string? UserId { get; set; }
    public Guid SessionId { get; init; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; }
}
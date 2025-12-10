using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record UserSessionMessage
{
    public required string ConnectionId { get; init; }

    public UserSession? Session { get; set; }
}

[MessagePackObject]
public record UserSession
{
    public string? UserId { get; set; }
    public Guid SessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public TimeSpan AccumulatedTime { get; set; }
}
using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record UserSessionMessage
{
    [Key(0)] public required string ConnectionId { get; init; }

    [Key(1)] public UserSession? Session { get; set; }
}

[MessagePackObject]
public record EntrancedUserData
{
    [Key(0)]
    public required Guid SessionId { get; set; }
}

[MessagePackObject]
public record UserSession
{
    [Key(0)] public string? UserId { get; set; }
    [Key(1)] public Guid SessionId { get; set; }
    [Key(2)] public DateTimeOffset StartedAt { get; set; }
    [Key(3)] public DateTimeOffset UpdatedAt { get; set; }
    [Key(4)] public TimeSpan AccumulatedTime { get; set; }
}
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
    [Key("sessionId")]
    public required Guid SessionId { get; set; }
}

[MessagePackObject]
public record UserSession
{
    [Key("userId")] public string? UserId { get; set; }
    [Key("sessionId")] public Guid SessionId { get; set; }
    [IgnoreMember] public DateTimeOffset StartedAt { get; set; }
    [IgnoreMember] public DateTimeOffset UpdatedAt { get; set; }
    [IgnoreMember] public TimeSpan AccumulatedTime { get; set; }
}
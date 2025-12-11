using MessagePack;
using ProtoBuf;

namespace StreamKey.Shared.DTOs;

[ProtoContract]
[MessagePackObject]
public record UserSessionMessage
{
    [ProtoMember(1)] public required string ConnectionId { get; init; }

    [ProtoMember(2)] public UserSession? Session { get; set; }
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
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

[ProtoContract]
[MessagePackObject]
public record UserSession
{
    [ProtoMember(1)] public string? UserId { get; set; }
    [ProtoMember(2)] public Guid SessionId { get; set; }
    [ProtoMember(3)] public DateTimeOffset StartedAt { get; set; }
    [ProtoMember(4)] public DateTimeOffset UpdatedAt { get; set; }
    [ProtoMember(5)] public TimeSpan AccumulatedTime { get; set; }
}
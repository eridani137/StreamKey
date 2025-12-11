using MessagePack;
using ProtoBuf;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[ProtoContract]
[MessagePackObject]
public record ClickChannelRequest
{
    [ProtoMember(1)] [Key("channelName")] public required string ChannelName { get; set; }
    [ProtoMember(2)] [Key("userId")] public required string UserId { get; set; }
}
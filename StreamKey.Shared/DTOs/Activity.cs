using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest
{
    [Key("channelName")] public required string ChannelName { get; set; }
    [Key("userId")] public required string UserId { get; set; }
}
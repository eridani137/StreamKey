using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest
{
    [Key("channelName")] public string ChannelName { get; init; } = string.Empty;
    [Key("userId")] public string UserId { get; init; } = string.Empty;
}
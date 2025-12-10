using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest(
    [property: Key("channelName")] string ChannelName,
    [property: Key("userId")] string UserId
);


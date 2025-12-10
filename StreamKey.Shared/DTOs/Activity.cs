using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest(
    [property: Key(0)] string ChannelName,
    [property: Key(1)] string UserId
);


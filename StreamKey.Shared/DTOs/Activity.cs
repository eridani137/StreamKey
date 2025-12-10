using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest(Guid SessionId, string UserId);

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest(string ChannelName, string UserId);


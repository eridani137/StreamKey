using MessagePack;

namespace StreamKey.Shared.DTOs;

public record UpdateUserActivityRequest
{
    [Obsolete("Не при SignalR соединении")] public Guid SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public record OnlineResponse(int OnlineUserCount);

[MessagePackObject]
public record ClickChannelRequest
{
    [Key("channelName")] public string ChannelName { get; init; } = string.Empty;
    [Key("userId")] public string UserId { get; init; } = string.Empty;
}
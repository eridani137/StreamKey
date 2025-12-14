using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record UpdateUserActivityRequest
{
    [Obsolete("Не нужен при SignalR соединении")]
    [Key("sessionId")]
    public Guid SessionId { get; set; }

    [Key("userId")] public string UserId { get; set; } = string.Empty;
}

public record OnlineResponse
{
    public int TotalOnline { get; init; }
    public int OldVersionsOnline { get; init; }
    public int Online { get; init; }
    public int ActiveOnline { get; init; }
    public int SleepingOnline { get; init; } 
}

[MessagePackObject]
public record ClickChannelRequest
{
    [Key("channelName")] public string ChannelName { get; init; } = string.Empty;
    [Key("userId")] public string UserId { get; init; } = string.Empty;
}
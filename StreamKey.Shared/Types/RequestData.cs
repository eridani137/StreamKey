namespace StreamKey.Shared.Types;

public record RequestData
{
    public required string ChannelName { get; init; }
    public required int ChannelId { get; set; }
    public required string UserIp { get; init; }
    public required string UserId { get; init; }
    public required string DeviceId { get; init; }
}
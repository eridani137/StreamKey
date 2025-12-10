namespace StreamKey.Shared.DTOs.Twitch;

public record UserTokenData
{
    public required string ChannelName { get; init; }
    public required int ChannelId { get; set; }
    public required string UserIp { get; init; }
    public required string UserId { get; init; }
    public required string DeviceId { get; init; }
}
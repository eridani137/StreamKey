namespace StreamKey.Shared.Types;

public class RequestData
{
    public required string ChannelName { get; set; }
    public required int ChannelId { get; set; }
    public required string UserIp { get; set; }
    public required string UserId { get; set; }
    public required string DeviceId { get; set; }
}
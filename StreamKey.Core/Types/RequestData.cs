namespace StreamKey.Core.Types;

public class RequestData
{
    public required string ChannelName { get; set; }
    public required int ChannelId { get; set; }
    public required string UserIp { get; set; }
    public required int UserId { get; set; }
}
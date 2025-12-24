namespace StreamKey.Shared.Entities;

public class ViewStatisticEntity : BaseIntEntity
{
    public string UserId { get; init; } = string.Empty;
    
    public string UserIp { get; init; } = string.Empty;
    
    public string ChannelName { get; init; } = string.Empty;
    
    public DateTime DateTime { get; init; } = DateTime.UtcNow;
}
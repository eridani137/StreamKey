namespace StreamKey.Shared.Entities;

public class ViewStatisticEntity : BaseIntEntity
{
    public string UserId { get; set; } = string.Empty;
    
    public string UserIp { get; set; } = string.Empty;
    
    public string ChannelName { get; set; } = string.Empty;
    
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
}
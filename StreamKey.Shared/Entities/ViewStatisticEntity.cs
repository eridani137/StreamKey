namespace StreamKey.Shared.Entities;

public class ViewStatisticEntity : BaseIntEntity
{
    public string UserId { get; set; } = string.Empty;
    public string UserIp { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    
    public int ChannelId { get; set; }
    
    public StatisticType Type { get; set; }
    
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
}

public enum StatisticType
{
    ViewStream = 0,
    ViewVod = 1
}
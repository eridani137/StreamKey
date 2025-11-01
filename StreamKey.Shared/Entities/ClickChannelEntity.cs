namespace StreamKey.Shared.Entities;

public class ClickChannelEntity : BaseIntEntity
{
    public required string ChannelName { get; set; }
    public required string UserId { get; set; }
    public DateTime DateTime { get; set; }
}
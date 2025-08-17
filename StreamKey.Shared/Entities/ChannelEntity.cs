namespace StreamKey.Shared.Entities;

public class ChannelEntity : BaseEntity
{
    public required string Name { get; set; }
    public required int Position { get; set; }
    public ChannelInfo? Info { get; set; }
}

public class ChannelInfo
{
    public required string Title { get; set; }
    public required string Thumb { get; set; }
    public required string Viewers { get; set; }
    public required string Description { get; set; }
    public DateTime UpdateTime { get; set; }
}
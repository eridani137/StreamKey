namespace StreamKey.Shared.Entities;

public class ChannelEntity : BaseEntity
{
    public required string Name { get; set; }
    public required int Position { get; set; }
}

public record ChannelInfo
{
    public required string Title { get; set; }
    public required string Thumb { get; set; }
    public required string Viewers { get; set; }
    public DateTime UpdateTime { get; set; }
}
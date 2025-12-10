using MessagePack;

namespace StreamKey.Shared.Entities;

public class ChannelEntity : BaseGuidEntity
{
    public required string Name { get; set; }
    public required int Position { get; set; }
    public ChannelInfo? Info { get; set; }
}

[MessagePackObject]
public class ChannelInfo
{
    public required string Title { get; set; }
    public required string Thumb { get; set; }
    public required string Viewers { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
}
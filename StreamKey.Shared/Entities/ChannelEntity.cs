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
    [Key(0)] public required string Title { get; set; }
    [Key(1)] public required string Thumb { get; set; }
    [Key(2)] public required string Viewers { get; set; }
    [Key(3)] public required string Description { get; set; }
    [Key(4)] public required string Category { get; set; }
}
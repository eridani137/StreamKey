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
    [Key("title")] public required string Title { get; set; }
    [Key("thumb")] public required string Thumb { get; set; }
    [Key("viewers")] public required string Viewers { get; set; }
    [Key("description")] public required string Description { get; set; }
    [Key("category")] public required string Category { get; set; }
}
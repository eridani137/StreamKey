using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ChannelDto
{
    [Key("channelName")] public required string ChannelName { get; set; }
    [Key("info")] public ChannelInfo? Info { get; set; }
    [Key("position")] public int Position { get; set; }
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
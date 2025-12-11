using MessagePack;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ChannelDto
{
    [Key("channelName")] public string ChannelName { get; init; } = string.Empty;
    [Key("info")] public ChannelInfo? Info { get; set; }
    [Key("position")] public int Position { get; init; }
}

[MessagePackObject]
public class ChannelInfo
{
    [Key("title")] public string Title { get; init; } = string.Empty;
    [Key("thumb")] public string Thumb { get; init; } = string.Empty;
    [Key("viewers")] public string Viewers { get; init; } = string.Empty;
    [Key("description")] public string Description { get; init; } = string.Empty;
    [Key("category")] public string Category { get; init; } = string.Empty;
}
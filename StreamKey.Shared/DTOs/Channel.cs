using MessagePack;
using ProtoBuf;
using StreamKey.Shared.Entities;

namespace StreamKey.Shared.DTOs;

[ProtoContract]
[MessagePackObject]
public record ChannelDto
{
    [ProtoMember(1)] [Key("channelName")] public required string ChannelName { get; set; }
    [ProtoMember(2)] [Key("info")] public ChannelInfo? Info { get; set; }
    [ProtoMember(3)] [Key("position")] public int Position { get; set; }
}

[ProtoContract]
[MessagePackObject]
public class ChannelInfo
{
    [ProtoMember(1)] [Key("title")] public required string Title { get; set; }
    [ProtoMember(2)] [Key("thumb")] public required string Thumb { get; set; }
    [ProtoMember(3)] [Key("viewers")] public required string Viewers { get; set; }
    [ProtoMember(4)] [Key("description")] public required string Description { get; set; }
    [ProtoMember(5)] [Key("category")] public required string Category { get; set; }
}
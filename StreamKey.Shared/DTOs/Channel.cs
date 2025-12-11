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
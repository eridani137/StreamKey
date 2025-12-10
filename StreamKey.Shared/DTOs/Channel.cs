using MessagePack;
using StreamKey.Shared.Entities;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ChannelDto(
    [property: Key(0)] string ChannelName,
    [property: Key(1)] ChannelInfo? Info,
    [property: Key(2)] int Position = 0
);
using MessagePack;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.DTOs;

[MessagePackObject]
public record ChannelDto(
    [property: Key("channelName")] string ChannelName,
    [property: Key("info")] ChannelInfo? Info,
    [property: Key("position")] int Position = 0
);

public record ClickChannelDto(string ChannelName, string UserId);
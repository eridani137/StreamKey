using MessagePack;
using StreamKey.Shared.Entities;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ChannelDto(
    [property: Key("channelName")] string ChannelName,
    [property: Key("info")] ChannelInfo? Info,
    [property: Key("position")] int Position = 0
);

[MessagePackObject]
public record ClickChannel(
    [property: Key("channelName")] string ChannelName,
    [property: Key("userId")] string UserId
);
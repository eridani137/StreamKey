using MessagePack;
using StreamKey.Shared.Entities;

namespace StreamKey.Shared.DTOs;

[MessagePackObject]
public record ChannelDto(
    string ChannelName,
    ChannelInfo? Info,
    int Position = 0
);
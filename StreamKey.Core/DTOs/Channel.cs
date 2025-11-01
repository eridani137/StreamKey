using StreamKey.Shared.Entities;

namespace StreamKey.Core.DTOs;

public record ChannelDto(string ChannelName, ChannelInfo? Info, int Position = 0);

public record ClickChannelDto(string ChannelName, string UserId);
using StreamKey.Core.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ChannelMapper
{
    public static ChannelDto Map(this ChannelEntity channel)
    {
        return new ChannelDto(channel.Name, channel.Position);
    }

    public static List<ChannelDto> Map(this IEnumerable<ChannelEntity> channels)
    {
        return channels.Select(Map).ToList();
    }
}
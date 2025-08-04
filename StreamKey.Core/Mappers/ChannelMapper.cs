using StreamKey.Core.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ChannelMapper
{
    public static ChannelDto Map(this ChannelEntity channel)
    {
        return new ChannelDto(channel.Name);
    }

    public static List<string> Map(this IEnumerable<ChannelEntity> channels)
    {
        return channels.Select(c => c.Name).ToList();
    }
}
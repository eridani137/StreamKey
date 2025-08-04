using StreamKey.Application.DTOs;
using StreamKey.Application.Entities;

namespace StreamKey.Application.Mappers;

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
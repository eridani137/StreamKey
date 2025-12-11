using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ChannelMapper
{
    extension(ChannelDto dto)
    {
        public ChannelEntity Map()
        {
            var channel = new ChannelEntity()
            {
                Name = dto.ChannelName,
                Position = dto.Position
            };
        
            return  channel;
        }
    }
    
    extension(ChannelEntity channel)
    {
        public ChannelDto Map()
        {
            return new ChannelDto()
            {
                ChannelName = channel.Name,
                Info = channel.Info,
                Position = channel.Position
            };
        }
    }

    extension(IEnumerable<ChannelEntity> channels)
    {
        public List<ChannelDto> Map()
        {
            return channels.Where(c => c.Info is not null).Select(Map).ToList();
        }

        public List<ChannelDto> MapAll()
        {
            return channels.Select(Map).ToList();
        }
    }
}
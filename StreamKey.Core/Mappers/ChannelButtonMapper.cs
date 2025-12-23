using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ChannelButtonMapper
{
    extension(ChannelButtonEntity entity)
    {
        public ChannelButtonDto Map()
        {
            return new ChannelButtonDto()
            {
                Html = entity.Html,
                Style = entity.Style,
                HoverStyle = entity.HoverStyle,
                ActiveStyle = entity.ActiveStyle,
                Link = entity.Link,
            };
        }
    }

    extension(ChannelButtonDto dto)
    {
        public ChannelButtonEntity Map()
        {
            return new ChannelButtonEntity()
            {
                Html = dto.Html,
                Style = dto.Style,
                HoverStyle = dto.HoverStyle,
                ActiveStyle = dto.ActiveStyle,
                Link = dto.Link
            };
        }
    }
}
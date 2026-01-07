using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ButtonMapper
{
    extension(ButtonEntity entity)
    {
        public ButtonDto Map()
        {
            return new ButtonDto()
            {
                Id = entity.Id,
                Html = entity.Html,
                Style = entity.Style,
                HoverStyle = entity.HoverStyle,
                ActiveStyle = entity.ActiveStyle,
                Link = entity.Link,
                IsEnabled = entity.IsEnabled
            };
        }
    }

    extension(ButtonDto dto)
    {
        public ButtonEntity Map()
        {
            return new ButtonEntity()
            {
                Html = dto.Html,
                Style = dto.Style,
                HoverStyle = dto.HoverStyle,
                ActiveStyle = dto.ActiveStyle,
                Link = dto.Link,
                IsEnabled = dto.IsEnabled
            };
        }
    }
}
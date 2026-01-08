using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ClickButtonMapper
{
    extension(ClickButtonRequest dto)
    {
        public ClickButtonEntity Map()
        {
            return new ClickButtonEntity()
            {
                Link = dto.Link,
                UserId = dto.UserId,
                DateTime = DateTime.UtcNow,
                Position = dto.Position,
            };
        }
    }
}
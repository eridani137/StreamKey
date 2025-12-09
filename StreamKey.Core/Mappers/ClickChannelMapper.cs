using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class ClickChannelMapper
{
    extension(ClickChannel dto)
    {
        public ClickChannelEntity Map()
        {
            return new ClickChannelEntity()
            {
                ChannelName = dto.ChannelName,
                UserId = dto.UserId,
                DateTime = DateTime.UtcNow
            };
        }
    }
}
using StreamKey.Core.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Mappers;

public static class TelegramUserMapper
{
    extension(TelegramAuthDto dto)
    {
        public TelegramUserEntity Map()
        {
            var telegramUser = new TelegramUserEntity()
            {
                TelegramId = dto.Id,
                FirstName = dto.FirstName,
                Username = dto.Username,
                AuthDate = dto.AuthDate,
                PhotoUrl = dto.PhotoUrl,
                Hash = dto.Hash
            };

            return telegramUser;
        }
    }

    extension(TelegramUserEntity entity)
    {
        public TelegramAuthDto Map()
        {
            var dto = new TelegramAuthDto()
            {
                Id = entity.TelegramId,
                FirstName = entity.FirstName,
                Username = entity.Username,
                AuthDate = entity.AuthDate,
                PhotoUrl = entity.PhotoUrl,
                Hash = entity.Hash
            };
            
            return dto;
        }
    }
}
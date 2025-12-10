using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;
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
        
        public TelegramUserDto MapUserDto(bool isChatMember)
        {
            return new TelegramUserDto()
            {
                Id = dto.Id, 
                Username = dto.Username,
                PhotoUrl = dto.PhotoUrl, 
                IsChatMember = isChatMember
            };
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

        public TelegramUserDto MapUserDto()
        {
            return new TelegramUserDto()
            {
                Id = entity.TelegramId, 
                Username = entity.Username,
                PhotoUrl = entity.PhotoUrl, 
                IsChatMember = entity.IsChatMember
            };
        }
    }
}
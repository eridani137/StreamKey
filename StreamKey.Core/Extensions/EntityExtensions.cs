using StreamKey.Shared.DTOs.Telegram;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Extensions;

public static class EntityExtensions
{
    extension(TelegramUserEntity user)
    {
        public void UpdateUserProperties(TelegramAuthDto dto, bool isChatMember)
        {
            user.FirstName = dto.FirstName;
            user.Username = dto.Username;
            user.AuthDate = dto.AuthDate;
            user.PhotoUrl = dto.PhotoUrl;
            user.Hash = dto.Hash;
            user.IsChatMember = isChatMember;
            user.AuthorizedAt = DateTime.UtcNow;
        }
    }
}
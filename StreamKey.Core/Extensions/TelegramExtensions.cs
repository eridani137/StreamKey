using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.Extensions;

public static class TelegramExtensions
{
    extension(GetChatMemberResponse response)
    {
        public bool IsChatMember()
        {
            return response.Result?.Status is ChatMemberStatus.Creator
                or ChatMemberStatus.Owner or ChatMemberStatus.Administrator or ChatMemberStatus.Member
                or ChatMemberStatus.Restricted;
        }
    }
}
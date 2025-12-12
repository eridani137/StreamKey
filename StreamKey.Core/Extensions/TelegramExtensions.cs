using Telegram.Bot.Types;

namespace StreamKey.Core.Extensions;

public static class TelegramExtensions
{
    extension(ChatMember? chatMember)
    {
        public bool IsChatMember()
        {
            if (chatMember == null) return false;
            return chatMember.IsAdmin || chatMember.IsInChat;
        }
    }
}
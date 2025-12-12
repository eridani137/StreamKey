using Telegram.Bot.Types;

namespace StreamKey.Core.Abstractions;

public interface ITelegramService
{
    Task<ChatMember?> GetChatMember(long userId, CancellationToken cancellationToken);
}
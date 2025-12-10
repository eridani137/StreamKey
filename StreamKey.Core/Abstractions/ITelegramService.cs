using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.Abstractions;

public interface ITelegramService
{
    Task<GetChatMemberResponse?> GetChatMember(long userId, CancellationToken cancellationToken);
}
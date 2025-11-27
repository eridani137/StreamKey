using StreamKey.Core.DTOs;

namespace StreamKey.Core.Abstractions;

public interface ITelegramService
{
    Task<GetChatMemberResponse?> GetChatMember(long userId);
}
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Shared;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace StreamKey.Core.Services;

public class TelegramService(ITelegramBotClient botClient, ILogger<TelegramService> logger) : ITelegramService
{
    private static readonly ChatId ChatId = new(ApplicationConstants.TelegramChatId);
    
    public async Task<ChatMember?> GetChatMember(long userId, CancellationToken cancellationToken)
    {
        try
        {
            var chatMember = await botClient.GetChatMember(ChatId, userId, cancellationToken: cancellationToken);
            return chatMember;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException e) when (e.Message.Contains("PARTICIPANT_ID_INVALID"))
        {
            logger.LogInformation(e, "PARTICIPANT_ID_INVALID [{UserId}]", userId);
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "GetChatMember [{UserId}]", userId);
            return null;
        }
    }
}
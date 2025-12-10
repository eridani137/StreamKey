using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.Services;

public class TelegramService(IHttpClientFactory clientFactory, ILogger<TelegramService> logger) : ITelegramService
{
    public async Task<GetChatMemberResponse?> GetChatMember(long userId, CancellationToken cancellationToken)
    {
        try
        {
            using var client = clientFactory.CreateClient(ApplicationConstants.TelegramClientName);
            using var response = await client.PostAsJsonAsync(
                $"/bot{ApplicationConstants.TelegramBotToken}/getChatMember", new
                {
                    chat_id = ApplicationConstants.TelegramChatId,
                    user_id = userId,
                }, cancellationToken: cancellationToken);

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var getChatMemberResponse =
                await JsonSerializer.DeserializeAsync<GetChatMemberResponse>(contentStream,
                    cancellationToken: cancellationToken);

            return getChatMemberResponse;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "GetChatMember");
            return null;
        }
    }
}
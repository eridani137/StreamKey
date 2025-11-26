using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamKey.Core.DTOs;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public interface ITelegramService
{
    Task<GetChatMemberResponse?> GetChatMember(long userId);
}

public class TelegramService(IHttpClientFactory clientFactory, ILogger<TelegramService> logger) : ITelegramService
{
    public async Task<GetChatMemberResponse?> GetChatMember(long userId)
    {
        try
        {
            using var client = clientFactory.CreateClient(ApplicationConstants.TelegramClientName);
            using var response = await client.PostAsJsonAsync($"/bot{ApplicationConstants.TelegramBotToken}/getChatMember", new
            {
                chat_id = ApplicationConstants.TelegramChatId,
                user_id = userId,
            });
            
            await using var contentStream = await response.Content.ReadAsStreamAsync();
            var getChatMemberResponse = await JsonSerializer.DeserializeAsync<GetChatMemberResponse>(contentStream);
        
            return getChatMemberResponse;
        }
        catch (Exception e)
        {
            logger.LogError(e, "GetChatMember");
            return null;
        }
    }
}
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs.TwitchGraphQL;
using StreamKey.Core.Extensions;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class TwitchService(IHttpClientFactory clientFactory, ILogger<TwitchService> logger) : ITwitchService
{
    public async Task<StreamPlaybackAccessTokenResponse?> GetStreamAccessToken(string username, HttpContext context)
    {
        var tokenRequest = new
        {
            operationName = "PlaybackAccessToken",
            variables = new
            {
                isLive = true,
                login = username,
                isVod = false,
                vodID = "",
                playerType = "site",
                platform = "web"
            },
            extensions = new
            {
                persistedQuery = new
                {
                    version = 1,
                    sha256Hash = "0828119ded1c13477966434e15800ff57ddacf13ba1911c129dc2200705b0712"
                }
            }
        };

        var tokenResponse =
            await SendTwitchGqlRequest<StreamPlaybackAccessTokenResponse>(tokenRequest, context, "StreamAccessToken");

        if (tokenResponse?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            tokenResponse.Data.StreamPlaybackAccessToken?.Value is null)
        {
            logger.LogError("Twitch вернул неверный StreamAccessToken. Response: {@Response}", tokenResponse);
            return null;
        }

        return tokenResponse;
    }

    public async Task<VideoPlaybackAccessTokenResponse?> GetVodAccessToken(string vodId, HttpContext context)
    {
        var tokenRequest = new
        {
            OperationName = "PlaybackAccessToken",
            variables = new
            {
                isLive = false,
                login = "",
                isVod = true,
                vodID = vodId,
                playerType = "site",
                зlatform = "web"
            },
            extensions = new
            {
                persistedQuery = new
                {
                    version = 1,
                    sha256Hash = "0828119ded1c13477966434e15800ff57ddacf13ba1911c129dc2200705b0712"
                }
            }
        };

        var tokenResponse = await SendTwitchGqlRequest<VideoPlaybackAccessTokenResponse>(tokenRequest, context, "VodAccessToken");

        if (tokenResponse?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            tokenResponse.Data?.VideoPlaybackAccessToken?.Value is null)
        {
            logger.LogError("Twitch вернул неверный VodAccessToken. Response: {@Response}", tokenResponse);
            return null;
        }

        return tokenResponse;
    }

    private async Task<T?> SendTwitchGqlRequest<T>(object tokenRequest, HttpContext context, string logPrefix)
        where T : class
    {
        using var client = clientFactory.CreateClient(ApplicationConstants.TwitchClientName);
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        {
            Content = JsonContent.Create(tokenRequest)
        };

        context.Request.Query.AddQueryAuth(request);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("{Prefix} Twitch GQL error {Status}. Body: {Body}", logPrefix, response.StatusCode, body);
            return null;
        }

        T? tokenResponse;
        try
        {
            tokenResponse = JsonSerializer.Deserialize<T>(body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "{Prefix} Ошибка десериализации Twitch JSON. Body: {Body}", logPrefix, body);
            return null;
        }

        return tokenResponse;
    }
}
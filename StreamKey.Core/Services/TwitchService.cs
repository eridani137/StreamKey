using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.TwitchGraphQL;
using StreamKey.Shared.Types;

namespace StreamKey.Core.Services;

public class TwitchService(IHttpClientFactory clientFactory, ILogger<TwitchService> logger) : ITwitchService
{
    public async Task<StreamPlaybackAccessTokenResponse?> GetStreamAccessToken(string username, string deviceId,
        HttpContext context)
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

        var result =
            await SendTwitchGqlRequest<StreamPlaybackAccessTokenResponse>(tokenRequest, deviceId, context,
                RequestTwitchPlaylistType.StreamAccessToken);

        if (result?.Data?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            result?.Data.Data.StreamPlaybackAccessToken?.Value is null)
        {
            logger.LogError("Twitch вернул неверный StreamAccessToken. Body: {@Body}", result?.RawJson);
            return null;
        }

        return result.Data;
    }

    public async Task<VideoPlaybackAccessTokenResponse?> GetVodAccessToken(string vodId, string deviceId,
        HttpContext context)
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

        var result =
            await SendTwitchGqlRequest<VideoPlaybackAccessTokenResponse>(tokenRequest, deviceId, context,
                RequestTwitchPlaylistType.VodAccessToken);

        if (result?.Data?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            result?.Data.Data?.VideoPlaybackAccessToken?.Value is null)
        {
            logger.LogError("Twitch вернул неверный VodAccessToken. Body: {@Body}", result?.RawJson);
            return null;
        }

        return result.Data;
    }

    private async Task<TwitchResponseWrapper<T>?> SendTwitchGqlRequest<T>(
        object tokenRequest,
        string deviceId,
        HttpContext context,
        RequestTwitchPlaylistType type)
        where T : class
    {
        using var client = clientFactory.CreateClient(ApplicationConstants.TwitchClientName);
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        {
            Content = JsonContent.Create(tokenRequest)
        };

        // if (type == RequestTwitchPlaylistType.StreamAccessToken)
        // {
        //     context.Request.Query.AddQueryDeviceId(request, deviceId);
        // }
        // else
        // {
        //     context.Request.Query.AddQueryAuthAndDeviceId(request, deviceId);
        // }
        
        context.Request.Query.AddQueryDeviceId(request, deviceId);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("{Prefix} Twitch GQL error {Status}. Body: {Body}",
                type, response.StatusCode, body);
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(body);
            return new TwitchResponseWrapper<T>(parsed, body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "{Prefix} Ошибка десериализации Twitch JSON. Body: {Body}",
                type, body);
            return null;
        }
    }
}
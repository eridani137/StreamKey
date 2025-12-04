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

        using var client = clientFactory.CreateClient(ApplicationConstants.ServerClientName);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        {
            Content = JsonContent.Create(tokenRequest)
        };
        context.Request.Query.AddQueryAuth(requestMessage);
        using var response = await client.SendAsync(requestMessage);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var accessTokenResponse =
            await JsonSerializer.DeserializeAsync<StreamPlaybackAccessTokenResponse>(contentStream);

        if (accessTokenResponse?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            accessTokenResponse.Data?.StreamPlaybackAccessToken?.Value is null)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            logger.LogError("Ошибка получения StreamAccessToken: {JSON}", jsonString);

            return null;
        }

        return accessTokenResponse;
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

        using var client = clientFactory.CreateClient(ApplicationConstants.ServerClientName);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        {
            Content = JsonContent.Create(tokenRequest)
        };
        context.Request.Query.AddQueryAuth(requestMessage);
        using var response = await client.SendAsync(requestMessage);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var accessTokenResponse =
            await JsonSerializer.DeserializeAsync<VideoPlaybackAccessTokenResponse>(contentStream);

        if (accessTokenResponse?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            accessTokenResponse.Data?.VideoPlaybackAccessToken?.Value is null)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            logger.LogError("Ошибка получения VodAccessToken: {JSON}", jsonString);

            return null;
        }

        return accessTokenResponse;
    }
}
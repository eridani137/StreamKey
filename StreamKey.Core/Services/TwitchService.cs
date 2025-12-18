using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Twitch;

namespace StreamKey.Core.Services;

public class TwitchService(IHttpClientFactory clientFactory, ILogger<TwitchService> logger) : ITwitchService
{
    public async Task<StreamPlaybackAccessTokenResponse?> GetStreamAccessToken(string username, string deviceId,
        HttpContext context)
    {
        var tokenRequest = new
        {
            operationName = "PlaybackAccessToken_Template",
            query =
                "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature   authorization { isForbidden forbiddenReasonCode }   __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature   __typename  }}",
            variables = new
            {
                isLive = true,
                login = username,
                isVod = false,
                vodID = "",
                playerType = "site",
                platform = "web"
            }
        };

        var result =
            await SendTwitchGqlRequest<StreamPlaybackAccessTokenResponse>(tokenRequest, deviceId, context,
                new RequestTwitchPlaylist()
                {
                    Type = RequestTwitchPlaylistType.StreamAccessToken,
                    Username = username,
                });

        if (result?.Data?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            result?.Data.Data.StreamPlaybackAccessToken?.Value is null)
        {
            // logger.LogWarning("GetStreamAccessToken: {Body}", result?.RawJson);
            return null;
        }

        return result.Data;
    }

    public async Task<VideoPlaybackAccessTokenResponse?> GetVodAccessToken(string vodId, string deviceId,
        HttpContext context)
    {
        var tokenRequest = new
        {
            operationName = "PlaybackAccessToken_Template",
            query = "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature   authorization { isForbidden forbiddenReasonCode }   __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature   __typename  }}",
            variables = new
            {
                isLive = false,
                login = "",
                isVod = true,
                vodID = vodId,
                playerType = "site",
                platform = "web"
            }
        };

        var result =
            await SendTwitchGqlRequest<VideoPlaybackAccessTokenResponse>(tokenRequest, deviceId, context,
                new RequestTwitchPlaylist()
                {
                    Type = RequestTwitchPlaylistType.VodAccessToken,
                    VodId = vodId
                });

        if (result?.Data?.Data?.VideoPlaybackAccessToken?.Signature is null ||
            result?.Data.Data?.VideoPlaybackAccessToken?.Value is null)
        {
            // logger.LogWarning("VodAccessToken: {Body}", result?.RawJson);
            return null;
        }

        return result.Data;
    }

    private async Task<TwitchResponseWrapper<T>?> SendTwitchGqlRequest<T>(
        object tokenRequest,
        string deviceId,
        HttpContext context,
        RequestTwitchPlaylist type)
        where T : class
    {
        using var client = clientFactory.CreateClient(ApplicationConstants.TwitchClientName);
        var request = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        {
            Content = JsonContent.Create(tokenRequest)
        };

        switch (type.Type)
        {
            case RequestTwitchPlaylistType.StreamAccessToken:
                context.Request.Query.AddQueryDeviceId(request, deviceId);
                break;
            case RequestTwitchPlaylistType.VodAccessToken:
                context.Request.Query.AddQueryAuthAndDeviceId(request, deviceId);
                break;
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            switch (type.Type)
            {
                case RequestTwitchPlaylistType.StreamAccessToken:
                    logger.LogWarning("{Type}: [{StatusCode}]: {Body}", type, response.StatusCode, body);
                    break;
                case RequestTwitchPlaylistType.VodAccessToken:
                    logger.LogWarning("{Type}: [{StatusCode}]: {Body}", type, response.StatusCode, body);
                    break;
            }
            
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(body);
            return new TwitchResponseWrapper<T>(parsed, body);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "{Prefix} [ошибка десериализации]: {Body}", type, body);
            return null;
        }
    }
}
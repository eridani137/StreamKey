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
        var tokenRequest = new PlaybackAccessTokenRequest
        {
            OperationName = "PlaybackAccessToken_Template",
            Query = "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature   authorization { isForbidden forbiddenReasonCode }   __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature   __typename  }}",
            Variables = new Variables
            {
                IsLive = true,
                Login = username,
                IsVod = false,
                VodId = "",
                PlayerType = "site",
                Platform = "web"
            }
        };

        using var client = clientFactory.CreateClient(ApplicationConstants.ServerClientName);
        // var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        // {
        //     Content = JsonContent.Create(tokenRequest)
        // };
        // context.Request.Query.AddQueryAuth(requestMessage);
        // using var response = await client.SendAsync(requestMessage); // TODO
        using var response = await client.PostAsJsonAsync(ApplicationConstants.QqlUrl, tokenRequest);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var accessTokenResponse = await JsonSerializer.DeserializeAsync<StreamPlaybackAccessTokenResponse>(contentStream);

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
        var tokenRequest = new PlaybackAccessTokenRequest
        {
            OperationName = "PlaybackAccessToken_Template",
            Query = "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature   authorization { isForbidden forbiddenReasonCode }   __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature   __typename  }}",
            Variables = new Variables
            {
                IsLive = false,
                Login = "",
                IsVod = true,
                VodId = vodId,
                PlayerType = "site",
                Platform = "web"
            }
        };
        
        using var client = clientFactory.CreateClient(ApplicationConstants.ServerClientName);
        // var requestMessage = new HttpRequestMessage(HttpMethod.Post, ApplicationConstants.QqlUrl)
        // {
        //     Content = JsonContent.Create(tokenRequest)
        // };
        // context.Request.Query.AddQueryAuth(requestMessage);
        // using var response = await client.SendAsync(requestMessage); // TODO
        using var response = await client.PostAsJsonAsync(ApplicationConstants.QqlUrl, tokenRequest);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var accessTokenResponse = await JsonSerializer.DeserializeAsync<VideoPlaybackAccessTokenResponse>(contentStream);

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
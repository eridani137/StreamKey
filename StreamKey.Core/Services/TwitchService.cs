using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs.TwitchGraphQL;
using StreamKey.Shared;

namespace StreamKey.Core.Services;

public class TwitchService(IHttpClientFactory clientFactory, ILogger<TwitchService> logger) : ITwitchService
{
    public async Task<PlaybackAccessTokenResponse?> GetAccessToken(string username)
    {
        var request = new PlaybackAccessTokenRequest
        {
            OperationName = "PlaybackAccessToken_Template",
            Query =
                "query PlaybackAccessToken_Template($login: String!, $isLive: Boolean!, $vodID: ID!, $isVod: Boolean!, $playerType: String!, $platform: String!) {  streamPlaybackAccessToken(channelName: $login, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isLive) {    value    signature   authorization { isForbidden forbiddenReasonCode }   __typename  }  videoPlaybackAccessToken(id: $vodID, params: {platform: $platform, playerBackend: \"mediaplayer\", playerType: $playerType}) @include(if: $isVod) {    value    signature   __typename  }}",
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
        using var response = await client.PostAsJsonAsync(ApplicationConstants.QqlUrl, request);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var accessTokenResponse = await JsonSerializer.DeserializeAsync<PlaybackAccessTokenResponse>(contentStream);

        if (accessTokenResponse?.Data?.StreamPlaybackAccessToken?.Signature is null ||
            accessTokenResponse?.Data?.StreamPlaybackAccessToken?.Value is null)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            logger.LogError("Ошибка получения [Signature, Value] JSON: {JSON}", jsonString);

            return null;
        }

        return accessTokenResponse;
    }
}
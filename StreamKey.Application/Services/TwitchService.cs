using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public interface ITwitchService
{
    Task<Result<string>> GetStreamSource(string username);
}

public class TwitchService(HttpClient client, IUsherService usherService, ILogger<TwitchService> logger) : ITwitchService
{
    public static readonly Uri QqlUrl = new("https://gql.twitch.tv/gql");
    public const string SiteUrl = "https://www.twitch.tv";
    public const string ClientId = "kimne78kx3ncx6brgo4mv6wki5h1ko";

    public static Dictionary<string, string> Headers { get; } = new()
    {
        { "Accept", "*/*" },
        { "Accept-Language", "en-US" },
        { "Client-ID", ClientId },
        { "Origin", SiteUrl },
        { "sec-ch-ua", """Not)A;Brand";v="8", "Chromium";v="138", "Google Chrome";v="138""" },
        { "sec-ch-ua-mobile", "?0" },
        { "sec-ch-ua-platform", """Windows""" },
        { "Sec-Fetch-Dest", "empty" },
        { "Sec-Fetch-Mode", "cors" },
        { "Sec-Fetch-Site", "same-site" },
        {
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36"
        },
    };

    public async Task<Result<string>> GetStreamSource(string username)
    {
        var accessToken = await GetAccessToken(username);
        if (accessToken is null) return Result.Failure<string>(Error.StreamNotFound);

        var hlsStreams = await usherService.GetHlsStreams(username, accessToken);
        return hlsStreams;
    }

    private async Task<PlaybackAccessTokenResponse?> GetAccessToken(string username)
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

        using var response = await client.PostAsJsonAsync(QqlUrl, request);
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
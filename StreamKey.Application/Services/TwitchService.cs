using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StreamKey.Application.DTOs;
using StreamKey.Application.DTOs.TwitchGraphQL;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Application.Services;

public class TwitchService(HttpClient client, IUsherService usherService, IMemoryCache cache, ILogger<TwitchService> logger) : ITwitchService
{
    public static Dictionary<string, string> Headers { get; } = new()
    {
        { "Accept", "*/*" },
        { "Accept-Language", "en-US" },
        { "Client-ID", StaticData.ClientId },
        { "Origin", StaticData.SiteUrl },
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

    private const string CacheKeyPrefix = "Username";
    private readonly TimeSpan _slidingExpiration = TimeSpan.FromSeconds(45);
    private readonly TimeSpan _absoluteExpiration = TimeSpan.FromMinutes(1);

    public async Task<Result<StreamResponseDto>> GetStreamSource(string username)
    {
        return await cache.GetOrCreateAsync($"{CacheKeyPrefix}:{username}", async entry =>
        {
            try
            {
                entry.SetSlidingExpiration(_slidingExpiration);
                entry.SetAbsoluteExpiration(_absoluteExpiration);
                
                logger.LogInformation("Получение стрима: {Username}", username);
            
                var accessToken = await GetAccessToken(username);
                if (accessToken is null) return Result.Failure<StreamResponseDto>(Error.StreamNotFound);
                
                return await usherService.Get1080PStream(username, accessToken);
            }
            catch (Exception e)
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.Zero;
                logger.LogError(e, "Ошибка возврата StreamResponseDto");
                return Result.Failure<StreamResponseDto>(Error.UnexpectedError);
            }
        }) ?? Result.Failure<StreamResponseDto>(Error.UnexpectedError);
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

        using var response = await client.PostAsJsonAsync(StaticData.QqlUrl, request);
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
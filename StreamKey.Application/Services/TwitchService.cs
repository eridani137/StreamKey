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
    private readonly TimeSpan _slidingExpiration = TimeSpan.FromSeconds(90);
    private readonly TimeSpan _absoluteExpiration = TimeSpan.FromMinutes(3);

    public async Task<Result<StreamResponseDto>> GetStreamSource(string username)
    {
        var cacheKey = $"{CacheKeyPrefix}:{username}";
        
        if (cache.TryGetValue(cacheKey, out StreamResponseDto? streamResponseDto))
        {
            if (streamResponseDto is not null)
            {
                logger.LogInformation("Стрим получен из кеша: {Username}", username);
                return Result.Success(streamResponseDto);
            }
        }

        try
        {
            logger.LogInformation("Получение стрима из API: {Username}", username);

            var accessToken = await GetAccessToken(username);
            if (accessToken is null)
            {
                logger.LogWarning("Не удалось получить токен доступа для пользователя: {Username}", username);
                return Result.Failure<StreamResponseDto>(Error.StreamNotFound);
            }

            var result = await usherService.Get1080PStream(username, accessToken);
            if (string.IsNullOrEmpty(result.Value.Source))
            {
                logger.LogWarning("Не удалось получить ссылку на видео поток: {Username}", username);
                return Result.Failure<StreamResponseDto>(Error.StreamNotFound);
            }

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(_slidingExpiration)
                .SetAbsoluteExpiration(_absoluteExpiration);

            cache.Set(cacheKey, result.Value, cacheOptions);
            
            logger.LogInformation("Стрим успешно получен и закеширован: {Username}", username);
            return result;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при получении стрима для пользователя: {Username}", username);
            return Result.Failure<StreamResponseDto>(Error.UnexpectedError);
        }
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
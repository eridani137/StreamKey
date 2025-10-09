using Carter;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Results;
using StreamKey.Core.Types;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;

namespace StreamKey.Api.Endpoints;

public class Playlist : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist")
            .WithTags("Работа с плейлистами");

        group.MapGet("", async (
                    HttpContext context,
                    IUsherService usherService,
                    IMemoryCache cache,
                    ISettingsStorage settings,
                    ILogger<Playlist> logger) =>
                await GetPlaylist(context, usherService, true, cache, settings, logger))
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithName("Получить плейлист");

        // group.MapGet("/server", async (
        //             HttpContext context,
        //             IUsherService usherService,
        //             IMemoryCache cache,
        //             ISettingsStorage settings,
        //             ILogger<Playlist> logger) =>
        //         await GetPlaylist(context, usherService, true, cache, settings, logger))
        //     .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
        //     .Produces(StatusCodes.Status400BadRequest)
        //     .Produces(StatusCodes.Status404NotFound)
        //     .Produces(StatusCodes.Status429TooManyRequests)
        //     .Produces(StatusCodes.Status500InternalServerError)
        //     .WithName("Получить серверный плейлист");
    }

    private static async Task<IResult> GetPlaylist(
        HttpContext context,
        IUsherService usherService,
        bool isServerPlaylist,
        IMemoryCache cache,
        ISettingsStorage settings,
        ILogger<Playlist> logger)
    {
        var queryString = context.Request.QueryString.ToString();

        try
        {
            var (statusCode, channelName, ip, rateLimit) = await RateLimit(context, cache, logger);

            // logger.LogInformation(
            //     isServerPlaylist
            //         ? "Получение серверного стрима: {Channel} [{Calls}]"
            //         : "Получение стрима: {Channel} [{Calls}]", channelName, rateLimit);

            var result =
                isServerPlaylist
                    ? await usherService.GetServerPlaylist(channelName)
                    : await usherService.GetPlaylist(channelName, queryString);

            if (result.IsFailure)
            {
                switch (result.Error.Code)
                {
                    case ErrorCode.StreamNotFound:
                        return Results.NotFound(result.Error.Message);
                    case ErrorCode.PlaylistNotReceived:
                        logger.LogWarning("{Channel}: {Error}", channelName, result.Error.Message);
                        return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                    case ErrorCode.None:
                    case ErrorCode.NullValue:
                    case ErrorCode.UnexpectedError:
                    case ErrorCode.Timeout:
                    default:
                        logger.LogWarning("{Error}: {Channel}", result.Error.Message, channelName);
                        return Results.InternalServerError(result.Error.Message);
                }
            }

            if (await settings.GetBoolSettingAsync(ApplicationConstants.LoggingPlaylists, false))
            {
                logger.LogInformation("{Playlist}", result.Value);
            }

            return Results.Content(result.Value, ApplicationConstants.PlaylistContentType);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Query: {Query}", queryString);
            return Results.InternalServerError();
        }
    }

    private static Task<(int StatusCode, string ChannelName, string Ip, int RateLimit)> RateLimit(HttpContext context,
        IMemoryCache cache, ILogger<Playlist> logger)
    {
        var queryString = context.Request.QueryString.ToString();

        if (!context.Request.Query.TryGetValue("token", out var tokenValue))
        {
            return Task.FromResult((400, "", "", -1));
        }

        var obj = JObject.Parse(tokenValue.ToString());
        var channel = obj.SelectToken(".channel")?.ToString();
        var ip = obj.SelectToken(".user_ip")?.ToString();

        if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(queryString))
        {
            logger.LogError("Не удалось получить channel: {Json}", obj.ToString());
            return Task.FromResult((400, "", "", -1));
        }

        if (string.IsNullOrEmpty(ip))
        {
            logger.LogError("Не удалось получить IP: {Json}", obj.ToString());
            return Task.FromResult((400, channel, "", -1));
        }

        var now = DateTime.UtcNow;
        if (!cache.TryGetValue<RateLimitInfo>(ip, out var rateLimit) || rateLimit == null)
        {
            rateLimit = new RateLimitInfo
            {
                Count = 1,
                ExpiresAt = now.AddSeconds(ApplicationConstants.TimeWindowSeconds),
                IsBanned = false
            };

            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
        }
        else if (rateLimit.IsBanned)
        {
            logger.LogWarning("[{Channel}] IP {IP} находится в бане", channel, ip);
            return Task.FromResult((429, channel, ip, rateLimit.Count));
        }
        else if (rateLimit.ExpiresAt <= now)
        {
            rateLimit.Count = 1;
            rateLimit.ExpiresAt = now.AddSeconds(ApplicationConstants.TimeWindowSeconds);
            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
        }
        else if (rateLimit.Count >= ApplicationConstants.MaxRequestsPerMinute)
        {
            logger.LogWarning("[{Channel}] IP {IP} превысил лимит, бан на 5 минут", channel, ip);

            rateLimit.IsBanned = true;
            rateLimit.ExpiresAt = now.AddMinutes(5);

            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);

            return Task.FromResult((429, channel, ip, rateLimit.Count));
        }
        else
        {
            rateLimit.Count++;
            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
        }

        return Task.FromResult((200, channel, ip, rateLimit.Count));
    }
}
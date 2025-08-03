using Carter;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using StreamKey.Application;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;
using StreamKey.Application.Types;

namespace StreamKey.Api.Endpoints;

public class Playlist : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/",
                async (HttpContext context, IUsherService usherService, IMemoryCache cache, ILogger<Playlist> logger) =>
                {
                    var queryString = context.Request.QueryString.ToString();
                    try
                    {
                        if (!context.Request.Query.TryGetValue("token", out var tokenValue))
                            return Results.BadRequest();

                        var obj = JObject.Parse(tokenValue.ToString());
                        var channel = obj.SelectToken(".channel")?.ToString();
                        var ip = obj.SelectToken(".user_ip")?.ToString();

                        if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(queryString))
                        {
                            logger.LogError("Не удалось получить channel: {Json}", obj.ToString());
                            return Results.BadRequest();
                        }

                        if (string.IsNullOrEmpty(ip))
                        {
                            logger.LogError("Не удалось получить IP: {Json}", obj.ToString());
                            return Results.BadRequest();
                        }

                        var now = DateTime.UtcNow;
                        if (!cache.TryGetValue<RateLimitInfo>(ip, out var rateLimit) || rateLimit == null)
                        {
                            rateLimit = new RateLimitInfo
                            {
                                Count = 1,
                                ExpiresAt = now.AddSeconds(StaticData.TimeWindowSeconds),
                                IsBanned = false
                            };

                            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
                        }
                        else if (rateLimit.IsBanned)
                        {
                            logger.LogWarning("[{Channel}] IP {IP} находится в бане", channel, ip);
                            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
                        }
                        else if (rateLimit.ExpiresAt <= now)
                        {
                            rateLimit.Count = 1;
                            rateLimit.ExpiresAt = now.AddSeconds(StaticData.TimeWindowSeconds);
                            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
                        }
                        else if (rateLimit.Count >= StaticData.MaxRequestsPerMinute)
                        {
                            logger.LogWarning("[{Channel}] IP {IP} превысил лимит, бан на 5 минут", channel, ip);

                            rateLimit.IsBanned = true;
                            rateLimit.ExpiresAt = now.AddMinutes(5);

                            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);

                            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
                        }
                        else
                        {
                            rateLimit.Count++;
                            cache.Set(ip, rateLimit, rateLimit.ExpiresAt);
                        }

                        logger.LogInformation("Получение стрима: {Channel} [{Calls}]", channel, rateLimit.Count);
                        var result = await usherService.GetPlaylist(channel, queryString);

                        if (result.IsFailure)
                        {
                            switch (result.Error.Code)
                            {
                                case ErrorCode.StreamNotFound:
                                    return Results.NotFound(result.Error.Message);
                                case ErrorCode.PlaylistNotReceived:
                                    logger.LogWarning("{Channel}: {Error}", channel, result.Error.Message);
                                    return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                                case ErrorCode.None:
                                case ErrorCode.NullValue:
                                case ErrorCode.UnexpectedError:
                                case ErrorCode.Timeout:
                                default:
                                    logger.LogWarning("{Error}: {Channel}", result.Error.Message, channel);
                                    return Results.InternalServerError(result.Error.Message);
                            }
                        }

                        return Results.Content(result.Value, StaticData.PlaylistContentType);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Query: {Query}", queryString);
                        return Results.InternalServerError();
                    }
                })
            .Produces<string>(contentType: StaticData.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status500InternalServerError);
    }
}
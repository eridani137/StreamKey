using System.Text;
using Carter;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Results;
using StreamKey.Core.Services;
using StreamKey.Core.Types;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

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
                    StatisticService statisticService,
                    ISettingsStorage settings,
                    ILogger<Playlist> logger) =>
                await GetStreamPlaylist(context, usherService, statisticService, settings, logger))
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Получить плейлист стрима");

        group.MapGet("/vod", async (
                    HttpContext context,
                    IUsherService usherService,
                    ISettingsStorage settings,
                    ILogger<Playlist> logger) =>
                await GetVodPlaylist(context, usherService, settings, logger))
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Получить плейлист записи");
    }

    private static async Task<IResult> GetStreamPlaylist(
        HttpContext context,
        IUsherService usherService,
        StatisticService statisticService,
        ISettingsStorage settings,
        ILogger<Playlist> logger)
    {
        try
        {
            var request = ProcessRequest(context, logger);
            if (request is null) return Results.BadRequest();
            
            statisticService.ViewStatisticQueue.Enqueue(new ViewStatisticEntity()
            {
                ChannelName = request.ChannelName,
                UserIp = request.UserIp,
                UserId = request.UserId
            });

            var result = await usherService.GetStreamPlaylist(request.ChannelName);

            if (result.IsFailure)
            {
                switch (result.Error.Code)
                {
                    case ErrorCode.StreamNotFound:
                        return Results.NotFound(result.Error.Message);
                    case ErrorCode.PlaylistNotReceived:
                        logger.LogWarning("{Channel}: {Error}", request.ChannelName, result.Error.Message);
                        return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                    case ErrorCode.None:
                    case ErrorCode.NullValue:
                    case ErrorCode.UnexpectedError:
                    case ErrorCode.Timeout:
                    default:
                        logger.LogWarning("{Error}: {Channel}", result.Error.Message, request.ChannelName);
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
            logger.LogError(e, "Query: {Query}", context.Request.QueryString.ToString());
            return Results.InternalServerError();
        }
    }

    private static async Task<IResult> GetVodPlaylist(
        HttpContext context,
        IUsherService usherService,
        ISettingsStorage settings,
        ILogger<Playlist> logger)
    {
        try
        {
            if (!context.Request.Query.TryGetValue("vod_id", out var vodId))
            {
                return Results.BadRequest("vod_id is not found");
            }
            
            // var request = ProcessRequest(context, logger);
            // if (request is null) return Results.BadRequest();
            
            var result = await usherService.GetVodPlaylist(vodId.ToString());
            
            if (result.IsFailure)
            {
                switch (result.Error.Code)
                {
                    case ErrorCode.StreamNotFound:
                        return Results.NotFound(result.Error.Message);
                    case ErrorCode.PlaylistNotReceived:
                        logger.LogWarning("{VodId}: {Error}", vodId.ToString(), result.Error.Message);
                        return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                    case ErrorCode.None:
                    case ErrorCode.NullValue:
                    case ErrorCode.UnexpectedError:
                    case ErrorCode.Timeout:
                    default:
                        logger.LogWarning("{Error}: {VodId}", result.Error.Message, vodId.ToString());
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
            logger.LogError(e, "Query: {Query}", context.Request.QueryString.ToString());
            return Results.InternalServerError();
        }
    }

    private static RequestData? ProcessRequest(HttpContext context, ILogger<Playlist> logger)
    {
        if (!context.Request.Query.TryGetValue("token", out var tokenValue))
        {
            logger.LogError("Token отсутствует в запросе");
            return null;
        }
        
        var obj = JObject.Parse(tokenValue.ToString());
        
        var channel = obj.SelectToken(".channel")?.ToString();
        var channelId = obj.SelectToken(".channel_id")?.ToObject<int>();
        
        var userIp = obj.SelectToken(".user_ip")?.ToString();
        var userId = obj.SelectToken(".user_id")?.ToString() ?? "anonymous";
        
        if (string.IsNullOrEmpty(channel))
        {
            logger.LogError("Не удалось получить channel: {Json}", obj.ToString());
            return null;
        }
        
        if (channelId is null or 0)
        {
            logger.LogError("Не удалось получить channel_id: {Json}", obj.ToString());
            return null;
        }

        if (string.IsNullOrEmpty(userIp))
        {
            logger.LogError("Не удалось получить IP: {Json}", obj.ToString());
            return null;
        }

        return new RequestData()
        {
            ChannelName = channel,
            ChannelId = channelId.Value,
            UserIp = userIp,
            UserId = userId
        };
    }
}
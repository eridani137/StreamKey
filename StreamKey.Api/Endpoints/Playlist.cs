using Carter;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Core.Results;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Types;

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
                    ILogger<Playlist> logger) =>
                await GetStreamPlaylist(context, usherService, statisticService, logger))
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status429TooManyRequests)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Получить плейлист стрима");

        group.MapGet("/vod", async (
                    HttpContext context,
                    IUsherService usherService,
                    ILogger<Playlist> logger) =>
                await GetVodPlaylist(context, usherService, logger))
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
        ILogger<Playlist> logger)
    {
        var request = ProcessRequest(context, logger);
        if (request is null) return Results.BadRequest();

        statisticService.ViewStatisticQueue.Enqueue(new ViewStatisticEntity()
        {
            ChannelName = request.ChannelName,
            UserIp = request.UserIp,
            UserId = request.UserId
        });

        var result = await usherService.GetStreamPlaylist(request.ChannelName, request.DeviceId, context);

        if (result.IsFailure)
        {
            switch (result.Error.Code)
            {
                case ErrorCode.StreamNotFound:
                    return Results.NotFound(result.Error.Message);
                case ErrorCode.PlaylistNotReceived:
                    logger.LogWarning("{Channel}: {Error}", request.ChannelName, result.Error.Message);
                    return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                default:
                    // logger.LogWarning("{Error}: {Channel}", result.Error.Message, request.ChannelName);
                    return Results.InternalServerError(result.Error.Message);
            }
        }

        return Results.Content(result.Value, ApplicationConstants.PlaylistContentType);
    }

    private static async Task<IResult> GetVodPlaylist(
        HttpContext context,
        IUsherService usherService,
        ILogger<Playlist> logger)
    {
        if (!context.Request.Query.TryGetValue("vod_id", out var vodId))
        {
            return Results.BadRequest("vod_id is not found");
        }

        if (!context.Request.Query.TryGetValue("token", out var tokenValue))
        {
            logger.LogError("Token отсутствует в запросе");
            return Results.BadRequest("token is not found");
        }

        var obj = JObject.Parse(tokenValue.ToString());

        var deviceId = obj.SelectToken(".device_id")?.ToString();

        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = TwitchExtensions.GenerateDeviceId();
        }

        var result = await usherService.GetVodPlaylist(vodId.ToString(), deviceId, context);

        if (result.IsFailure)
        {
            switch (result.Error.Code)
            {
                case ErrorCode.StreamNotFound:
                    return Results.NotFound(result.Error.Message);
                case ErrorCode.PlaylistNotReceived:
                    logger.LogWarning("{VodId}: {Error}", vodId.ToString(), result.Error.Message);
                    return Results.Content(result.Error.Message, statusCode: result.Error.StatusCode);
                default:
                    // logger.LogWarning("{Error}: {VodId}", result.Error.Message, vodId.ToString());
                    return Results.InternalServerError(result.Error.Message);
            }
        }

        return Results.Content(result.Value, ApplicationConstants.PlaylistContentType);
    }

    private static RequestData? ProcessRequest(HttpContext context, ILogger<Playlist> logger)
    {
        if (!context.Request.Query.TryGetValue("token", out var tokenValue))
        {
            logger.LogError("Token отсутствует в запросе");
            return null;
        }

        var obj = JObject.Parse(tokenValue.ToString());

        var deviceId = obj.SelectToken(".device_id")?.ToString();

        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = TwitchExtensions.GenerateDeviceId();
        }

        var channel = obj.SelectToken(".channel")?.ToString();
        var channelId = obj.SelectToken(".channel_id")?.ToObject<int>();

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

        var userIp = obj.SelectToken(".user_ip")?.ToString();
        var userId = obj.SelectToken(".user_id")?.ToString() ?? "anonymous";

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
            UserId = userId,
            DeviceId = deviceId
        };
    }
}
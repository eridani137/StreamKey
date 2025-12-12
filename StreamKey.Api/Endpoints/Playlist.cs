using System.Net;
using System.Text;
using Carter;
using Newtonsoft.Json.Linq;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Twitch;
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
                ILogger<Playlist> logger) =>
            {
                var request = ProcessRequest(context, logger);
                if (request is null) return Results.BadRequest();

                statisticService.ViewStatisticQueue.Enqueue(new ViewStatisticEntity()
                {
                    ChannelName = request.ChannelName,
                    UserIp = request.UserIp,
                    UserId = request.UserId
                });

                var response = await usherService.GetStreamPlaylist(request.ChannelName, request.DeviceId, context);
                if (response is null) return Results.BadRequest();

                if (!response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.NotFound)
                {
                    var body = await response.Content.ReadAsByteArrayAsync();
                    var bodyString = Encoding.UTF8.GetString(body);

                    logger.LogWarning("GetStream {ChannelName} {StatusCode}: {Body}", request.ChannelName,
                        (int)response.StatusCode, bodyString);

                    await WriteHttpResponse(context, response, body);
                    return Results.Empty;
                }

                await WriteHttpResponse(context, response);
                return Results.Empty;
            })
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Получить плейлист стрима");

        group.MapGet("/vod", async (
                HttpContext context,
                IUsherService usherService,
                ILogger<Playlist> logger) =>
            {
                if (!context.Request.Query.TryGetValue("vod_id", out var vodIdValue) ||
                    vodIdValue.ToString() is not { } vodId)
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

                var response = await usherService.GetVodPlaylist(vodId, deviceId, context);
                if (response is null) return Results.NotFound();

                if (!response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.Forbidden &&
                    response.StatusCode != HttpStatusCode.NotFound)
                {
                    var body = await response.Content.ReadAsByteArrayAsync();
                    var bodyString = Encoding.UTF8.GetString(body);

                    logger.LogWarning("GetVod {VodId} {StatusCode}: {Body}", vodId, (int)response.StatusCode,
                        bodyString);

                    await WriteHttpResponse(context, response, body);
                    return Results.Empty;
                }

                await WriteHttpResponse(context, response);
                return Results.Empty;
            })
            .Produces<string>(contentType: ApplicationConstants.PlaylistContentType)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithSummary("Получить плейлист записи");
    }

    private static async Task WriteHttpResponse(
        HttpContext context,
        HttpResponseMessage response,
        byte[]? bodyOverride = null)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        context.Response.Headers.Remove("transfer-encoding");
        context.Response.Headers.Remove("Content-Length");

        if (bodyOverride != null)
        {
            await context.Response.Body.WriteAsync(bodyOverride);
        }
        else
        {
            await response.Content.CopyToAsync(context.Response.Body);
        }
    }

    private static UserTokenData? ProcessRequest(HttpContext context, ILogger<Playlist> logger)
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

        return new UserTokenData()
        {
            ChannelName = channel,
            ChannelId = channelId.Value,
            UserIp = userIp,
            UserId = userId,
            DeviceId = deviceId
        };
    }
}
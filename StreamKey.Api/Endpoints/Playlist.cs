using System.Net;
using System.Text;
using System.Text.Json;
using Carter;
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
                var userToken = ProcessUserToken(context, logger);
                if (userToken is null) return Results.BadRequest();

                statisticService.ViewStatisticQueue.Enqueue(new ViewStatisticEntity()
                {
                    ChannelName = userToken.ChannelName,
                    UserIp = userToken.UserIp,
                    UserId = userToken.UserId
                });

                var response = await usherService.GetStreamPlaylist(userToken.ChannelName, userToken.DeviceId, context);
                if (response is null) return Results.BadRequest();

                if (!response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.NotFound)
                {
                    var body = await response.Content.ReadAsByteArrayAsync();
                    var bodyString = Encoding.UTF8.GetString(body);

                    logger.LogWarning("GetStream {ChannelName} [{StatusCode}]: {Body}", userToken.ChannelName,
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

                var userToken = ProcessUserToken(context, logger);
                if (userToken is null) return Results.BadRequest();

                var response = await usherService.GetVodPlaylist(vodId, userToken.DeviceId, context);
                if (response is null) return Results.NotFound();

                if (!response.IsSuccessStatusCode &&
                    response.StatusCode != HttpStatusCode.Forbidden &&
                    response.StatusCode != HttpStatusCode.NotFound)
                {
                    var body = await response.Content.ReadAsByteArrayAsync();
                    var bodyString = Encoding.UTF8.GetString(body);

                    logger.LogWarning("GetVod {VodId} [{StatusCode}]: {Body}", vodId, (int)response.StatusCode,
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

    private static UserTokenData? ProcessUserToken(HttpContext context, ILogger<Playlist> logger)
    {
        if (!context.Request.Query.TryGetValue("token", out var tokenValue) || tokenValue.ToString() is not { } token)
        {
            logger.LogError("Token отсутствует в запросе");
            return null;
        }

        using var doc = JsonDocument.Parse(token);
        var root = doc.RootElement;

        var deviceId = root.TryGetProperty("device_id", out var deviceIdProp) &&
                       deviceIdProp.ValueKind == JsonValueKind.String
            ? deviceIdProp.GetString() ?? ""
            : TwitchExtensions.GenerateDeviceId();

        var channel = root.TryGetProperty("channel", out var channelProp) &&
                      channelProp.ValueKind == JsonValueKind.String
            ? channelProp.GetString() ?? ""
            : "null";

        var channelId = root.TryGetProperty("channel_id", out var channelIdProp) &&
                        channelIdProp.ValueKind == JsonValueKind.Number
            ? channelIdProp.GetInt32()
            : -1;

        var userIp = root.TryGetProperty("user_ip", out var userIpProp) &&
                     userIpProp.ValueKind == JsonValueKind.String
            ? userIpProp.GetString() ?? ""
            : "null";

        var userId = root.TryGetProperty("user_id", out var userIdProp) &&
                     userIdProp.ValueKind == JsonValueKind.String
            ? userIdProp.GetString() ?? "anonymous"
            : "anonymous";

        return new UserTokenData()
        {
            ChannelName = channel,
            ChannelId = channelId,
            UserIp = userIp,
            UserId = userId,
            DeviceId = deviceId
        };
    }
}
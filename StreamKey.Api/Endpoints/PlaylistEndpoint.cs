using Carter;
using Newtonsoft.Json.Linq;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Api.Endpoints;

public class PlaylistEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/", async (HttpContext context, IUsherService usherService, ILogger<PlaylistEndpoint> logger) =>
            {
                var queryString = context.Request.QueryString.ToString();
                if (!context.Request.Query.TryGetValue("token", out var tokenValue)) return Results.BadRequest();

                var obj = JObject.Parse(tokenValue.ToString());
                var channel = obj.SelectToken(".channel")?.ToString();

                if (string.IsNullOrEmpty(channel) || string.IsNullOrEmpty(queryString))
                {
                    logger.LogError("Не удалось получить channel: {Json}", obj.ToString());
                    return Results.BadRequest();
                }

                logger.LogInformation("Получение стрима: {Channel}", channel);
                var result = await usherService.GetPlaylist(channel, queryString);

                if (result.IsFailure)
                {
                    logger.LogError("Ошибка получения стрима {Channel}: {Error}", channel, result.Error.Message);

                    return result.Error.Code switch
                    {
                        ErrorCode.StreamNotFound => Results.NotFound(result.Error.Message),
                        _ => Results.BadRequest(result.Error.Message)
                    };
                }

                // logger.LogInformation("Плейлист успешно отправлен: {Channel}\n{@Playlist}", channel, result.Value);
                return Results.Content(result.Value, "application/vnd.apple.mpegurl");
            })
            .Produces<string>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
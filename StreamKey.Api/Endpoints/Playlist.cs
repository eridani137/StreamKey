using Carter;
using Newtonsoft.Json.Linq;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;

namespace StreamKey.Api.Endpoints;

public class Playlist : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/", async (HttpContext context, IUsherService usherService, ILogger<Playlist> logger) =>
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
                    switch (result.Error.Code)
                    {
                        case ErrorCode.StreamNotFound:
                            return Results.NotFound(result.Error.Message);
                        case ErrorCode.None:
                        case ErrorCode.NullValue:
                        case ErrorCode.PlaylistNotReceived:
                        case ErrorCode.UnexpectedError:
                        case ErrorCode.Timeout:
                            return Results.Problem(result.Error.Message);
                        default:
                            logger.LogError("{Error}: {Channel}", result.Error.Message, channel);
                            return Results.BadRequest(result.Error.Message);
                    }
                }

                return Results.Content(result.Value, "application/vnd.apple.mpegurl");
            })
            .Produces<string>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
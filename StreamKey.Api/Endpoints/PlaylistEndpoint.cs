using Carter;
using Newtonsoft.Json.Linq;
using StreamKey.Application.Interfaces;

namespace StreamKey.Api.Endpoints;

public class PlaylistEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/", async (HttpContext context, ITwitchService twitchService) =>
        {
            var queryString = context.Request.QueryString.ToString();
            if (!context.Request.Query.TryGetValue("token", out var tokenValue)) return Results.BadRequest();
            var obj = JObject.Parse(tokenValue.ToString());
            var username = obj.SelectToken(".channel")?.ToString();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(queryString)) return Results.BadRequest();
            var playlist = await twitchService.GetPlaylist(username, queryString);

            return Results.Ok(playlist.Value);
        });
    }
}
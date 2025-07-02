using Carter;

namespace StreamKey.Api.Endpoints;

public class PlaylistEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/playlist");

        group.MapGet("/", (HttpContext context) =>
        {
            var queryParams = context.Request.Query;
        });
    }
}
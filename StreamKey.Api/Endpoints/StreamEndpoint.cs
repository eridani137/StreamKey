using Carter;

namespace StreamKey.Api.Endpoints;

public class StreamEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stream");

        group.MapPost("get", () =>
            {
                return Results.Ok("test");
            })
        .Produces<string>()
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
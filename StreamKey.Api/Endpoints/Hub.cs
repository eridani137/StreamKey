using Carter;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Hub : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hub");

        group.MapGet("/connections",
                async (IConnectionStore store) =>
                {
                    var connections = await store.GetAllActiveConnectionsAsync();
                    return Results.Json(connections);
                })
            .WithSummary("Получить соединения");
        
        group.MapGet("/online",
                async (IConnectionStore store) =>
                {
                    var connections = await store.GetAllActiveConnectionsAsync();
                    return Results.Ok(new OnlineResponse(connections.Count));
                })
            .WithSummary("Получить онлайн");
    }
}
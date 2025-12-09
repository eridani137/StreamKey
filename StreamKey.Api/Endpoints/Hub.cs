using Carter;
using Microsoft.AspNetCore.Mvc;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Hub : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hub");

        group.MapGet("/connections",
                async ([FromServices] IConnectionStore store) =>
                {
                    var connections = await store.GetAllActiveConnectionsAsync();
                    return Results.Json(connections);
                })
            .WithSummary("Получить соединения");
        
        group.MapGet("/online",
                async ([FromServices] IConnectionStore store) =>
                {
                    var connections = await store.GetAllActiveConnectionsAsync();
                    return Results.Ok(new OnlineResponse(connections.Count));
                })
            .WithSummary("Получить онлайн");
        
        group.MapGet("/disconnected",
                async ([FromServices] IConnectionStore store) =>
                {
                    var disconnected = await store.GetAllDisconnectedConnectionsAsync();
                    return Results.Json(disconnected);
                })
            .WithSummary("Получить отключившихся пользователей");
    }
}
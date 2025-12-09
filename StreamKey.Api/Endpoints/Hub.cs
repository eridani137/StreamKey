using Carter;
using StreamKey.Core.Stores;

namespace StreamKey.Api.Endpoints;

public class Hub : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hub");

        group.MapGet("/connections", () => Results.Json(ConnectionRegistry.GetAllActive()))
            .WithSummary("Получить соединения");
        
        group.MapGet("/online", () => Results.Json(ConnectionRegistry.GetAllActive().Count()))
            .WithSummary("Получить онлайн");
        
        group.MapGet("/disconnected", () => Results.Json(ConnectionRegistry.GetAllDisconnected()))
            .WithSummary("Получить отключившихся пользователей");
    }
}
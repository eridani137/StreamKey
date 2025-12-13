using Carter;
using StreamKey.Core;
using StreamKey.Core.Common;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Hub : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hub")
            .RequireAuthorization()
            .WithTags("Hub");

        group.MapGet("/connections", () => Results.Json(ConnectionRegistry.GetAllActive()))
            .WithSummary("Получить соединения");
        
        group.MapGet("/online", () => Results.Ok(ConnectionRegistry.GetAllActive().Count()))
            .WithSummary("Получить онлайн");
        
        group.MapGet("/disconnected", () => Results.Json(ConnectionRegistry.GetAllDisconnected()))
            .WithSummary("Получить отключившихся пользователей");
    }
}
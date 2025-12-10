using Carter;
using Microsoft.AspNetCore.Mvc;
using StreamKey.Core.Abstractions;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Camoufox : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("camoufox")
            .RequireAuthorization()
            .WithTags("Camoufox");

        group.MapPost("/html",
                async ([FromBody] CamoufoxRequest dto, ICamoufoxService service) =>
                {
                    var result = await service.GetPageHtml(dto);
                    return Results.Ok(result);
                })
            .WithSummary("Получить HTML");

        group.MapPost("/screenshot",
                async ([FromBody] CamoufoxRequest dto, ICamoufoxService service) =>
                {
                    var screenshot = await service.GetPageScreenshot(dto);
                    return screenshot is null
                        ? Results.Empty
                        : Results.File(screenshot, "image/png");
                })
            .Produces(StatusCodes.Status200OK)
            .WithSummary("Получить скриншот");
    }
}
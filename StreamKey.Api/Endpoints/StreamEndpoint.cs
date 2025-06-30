using Carter;
using StreamKey.Application.DTOs;
using StreamKey.Application.Interfaces;

namespace StreamKey.Api.Endpoints;

public class StreamEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stream");

        group.MapPost("get", async (StreamReceiveDto dto, IStreamService service) =>
            {
                var source = await service.GetSource(dto.Username);
                
                if (source is null) return Results.NotFound();
                
                return Results.Ok(source);
            })
            .Produces<string>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}
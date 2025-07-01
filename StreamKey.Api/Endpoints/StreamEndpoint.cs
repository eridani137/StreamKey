using Carter;
using StreamKey.Application.DTOs;
using StreamKey.Application.Results;
using StreamKey.Application.Services;

namespace StreamKey.Api.Endpoints;

public class StreamEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stream");

        group.MapPost("get", async (StreamRequestDto dto, ITwitchService service) =>
            {
                var result = await service.GetStreamSource(dto.Username);

                if (result.IsFailure)
                {
                    return result.Error.Code switch
                    {
                        ErrorCode.StreamNotFound => Results.NotFound(result.Error.Message),
                        _ => Results.BadRequest(result.Error.Message)
                    };
                }

                return Results.Ok(result.Value);
            })
            .Produces<string>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
    }
}
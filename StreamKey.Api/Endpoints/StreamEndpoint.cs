using Carter;
using StreamKey.Application.DTOs;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Results;
using StreamKey.Application.Services;

namespace StreamKey.Api.Endpoints;

public class StreamEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stream");

        group.MapPost("get", async (StreamRequestDto dto, ITwitchService service, ILogger<StreamEndpoint> logger) =>
            {
                var result = await service.GetStreamSource(dto.Username);

                if (result.IsFailure)
                {
                    logger.LogError("Ошибка получения стрима {Username}: {Error}", dto.Username, result.Error.Message);
                    
                    return result.Error.Code switch
                    {
                        ErrorCode.StreamNotFound or ErrorCode.NotFound1080P => Results.NotFound(result.Error.Message),
                        _ => Results.BadRequest(result.Error.Message)
                    };
                }
                
                logger.LogInformation("Стрим отправлен: {Username}", dto.Username);

                return Results.Ok(result.Value);
            })
            .Produces<StreamResponseDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
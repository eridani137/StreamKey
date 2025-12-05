using Carter;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Api.Endpoints;

public class Channel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/channels")
            .WithTags("Работа с каналами")
            .RequireAuthorization();

        group.MapGet("/all",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();
                    var mapped = channels.MapAll();

                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelDto>>()
            .WithSummary("Получить все добавленные каналы");

        group.MapPost("",
                async (ChannelDto dto, IChannelService service) =>
                {
                    var result = await service.AddChannel(dto);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .AddEndpointFilter<ValidationFilter<ChannelDto>>()
            .Produces<ChannelDto>()
            .WithSummary("Добавить канал");

        group.MapDelete("/{position:int}",
                async (int position, IChannelService service) =>
                {
                    var result = await service.RemoveChannel(position);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ChannelDto>()
            .WithSummary("Удалить канал");

        group.MapPut("",
                async (ChannelDto dto, IChannelService service) =>
                {
                    var result = await service.UpdateChannel(dto);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value.Map());
                })
            .Produces<ChannelDto>()
            .WithSummary("Обновить канал");
    }
}
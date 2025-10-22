using Carter;
using Microsoft.AspNetCore.Mvc;
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

        group.MapGet("/refresh",
                async (ILogger<Channel> logger, IChannelRepository repository, IChannelService service) =>
                {
                    var channels = await repository.GetAll();
                    foreach (var channel in channels)
                    {
                        try
                        {
                            await service.UpdateChannelInfo(channel);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Ошибка при обновлении информации канала {@Channel}", channel);
                        }
                    }
                })
            .Produces(StatusCodes.Status200OK)
            .RequireAuthorization()
            .WithName("Запуск обновления каналов");

        group.MapGet("",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();
                    var mapped = channels.Map();

                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelDto>>()
            .AllowAnonymous()
            .WithName("Получить онлайн каналы");

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
            .WithName("Добавить канал");

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
            .WithName("Удалить канал");

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
            .WithName("Обновить канал");
    }
}
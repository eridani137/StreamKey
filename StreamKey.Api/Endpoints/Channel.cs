using Carter;
using Microsoft.AspNetCore.Mvc;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Channel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/channels")
            .WithTags("Работа с каналами")
            .RequireAuthorization();

        group.MapPost("/click",
                (ClickChannelDto dto, StatisticService service) =>
                {
                    service.ChannelActivityQueue.Enqueue(new ClickChannelEntity()
                    {
                        ChannelName = dto.ChannelName,
                        UserId = dto.UserId,
                        DateTime = DateTime.UtcNow
                    });
                })
            .AllowAnonymous()
            .WithSummary("Клик на канал");

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
            .WithSummary("Запуск обновления каналов");

        group.MapGet("/all",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();
                    var mapped = channels.MapAll();

                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelDto>>()
            .WithSummary("Получить все добавленные каналы");

        group.MapGet("",
                async (IChannelService service) =>
                {
                    var channels = await service.GetChannels();
                    var mapped = channels.Map();

                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelDto>>()
            .AllowAnonymous()
            .WithSummary("Получить онлайн каналы");

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
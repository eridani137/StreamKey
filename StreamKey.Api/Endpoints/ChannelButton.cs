using Carter;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class ChannelButton : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/channel-buttons")
            .WithTags("Работа с кнопками")
            .RequireAuthorization();

        group.MapGet("",
                async (IChannelButtonService service, CancellationToken cancellationToken) =>
                {
                    var buttons = await service.GetChannelButtons(cancellationToken);
                    var mapped = buttons.Select(b => b.Map());

                    return Results.Ok(mapped);
                })
            .Produces<List<ChannelButtonDto>>()
            .WithSummary("Получить все добавленные кнопки");

        group.MapPost("",
                async (ChannelButtonDto dto, IChannelButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.AddChannelButton(dto, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ChannelButtonDto>()
            .WithSummary("Добавить кнопку");

        group.MapDelete("/{link}",
                async (string link, IChannelButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.RemoveChannelButton(link, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ChannelButtonDto>()
            .WithSummary("Удалить кнопку");

        group.MapPut("",
                async (ChannelButtonDto dto, IChannelButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.UpdateChannelButton(dto, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value.Map());
                })
            .Produces<ChannelButtonDto>()
            .WithSummary("Обновить кнопку");
    }
}
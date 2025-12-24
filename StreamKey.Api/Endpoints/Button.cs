using Carter;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Button : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/buttons")
            .WithTags("Работа с кнопками")
            .RequireAuthorization();

        group.MapGet("",
                async (IButtonService service, CancellationToken cancellationToken) =>
                {
                    var buttons = await service.GetButtons(cancellationToken);
                    var mapped = buttons.Select(b => b.Map());

                    return Results.Ok(mapped);
                })
            .Produces<List<ButtonDto>>()
            .WithSummary("Получить все добавленные кнопки");

        group.MapPost("",
                async (ButtonDto dto, IButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.AddButton(dto, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ButtonDto>()
            .WithSummary("Добавить кнопку");

        group.MapDelete("/{link}",
                async (string link, IButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.RemoveButton(link, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value);
                })
            .Produces<ButtonDto>()
            .WithSummary("Удалить кнопку");

        group.MapPut("",
                async (ButtonDto dto, IButtonService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.UpdateButton(dto, cancellationToken);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(detail: result.Error.Message, statusCode: result.Error.StatusCode);
                    }

                    return Results.Ok(result.Value.Map());
                })
            .Produces<ButtonDto>()
            .WithSummary("Обновить кнопку");
    }
}
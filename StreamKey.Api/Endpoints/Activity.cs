using Carter;
using StreamKey.Core.Services;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Activity : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/activity")
            .WithTags("Активность");

        group.MapPost("/update",
                (UpdateUserActivityRequest updateUserActivityRequest, StatisticService statisticService) =>
                {
                    statisticService.UpdateUserActivity(updateUserActivityRequest);

                    return Results.Ok();
                })
            .WithSummary("Обновление активности пользователя");

        group.MapPost("/click",
                (ClickChannelRequest dto, StatisticService service) =>
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
    }
}
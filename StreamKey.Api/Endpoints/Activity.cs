using Carter;
using StreamKey.Core.DTOs;
using StreamKey.Core.Services;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Activity : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/activity")
            .WithTags("Активность");

        group.MapPost("/update",
                (ActivityRequest activityRequest, StatisticService statisticService) =>
                {
                    statisticService.UpdateUserActivity(activityRequest);

                    return Results.Ok();
                })
            .WithSummary("Обновление активности пользователя");

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
    }
}
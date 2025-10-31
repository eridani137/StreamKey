using Carter;
using StreamKey.Core.DTOs;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Api.Endpoints;

public class Statistic : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/statistic")
            .WithTags("Статистические данные");

        group.MapGet("/channels",
                async (StatisticRepository repository) => Results.Json(await repository.GetTop10ViewedChannelsAsync()))
            .WithDescription("Топ 10 каналов")
            .RequireAuthorization();

        group.MapPost("/activity/update",
                (ActivityRequest activityRequest, StatisticService statisticService) =>
                {
                    statisticService.UpdateUserActivity(activityRequest);

                    return Results.Ok();
                })
            .WithDescription("Обновление активности пользователя");

        group.MapGet("/activity",
                (StatisticService statisticService) =>
                    Results.Ok(new ActivityResponse(statisticService.OnlineUsers.Count)))
            .WithDescription("Получить количество онлайн пользователей")
            .RequireAuthorization()
            .Produces<ActivityResponse>();
    }
}
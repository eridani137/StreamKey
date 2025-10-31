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
                async (int hours, int count, ViewStatisticRepository repository) =>
                {
                    if (hours <= 0 || count <= 0) return Results.BadRequest("Часы и количество записей должны быть больше 0");
                    
                    return Results.Json(await repository.GetTopViewedChannelsAsync(hours, count));
                })
            .WithDescription("Топ 10 каналов")
            .RequireAuthorization();

        var activityGroup = app.MapGroup("/activity")
            .WithTags("Текущий онлайн");

        activityGroup.MapPost("/update",
                (ActivityRequest activityRequest, StatisticService statisticService) =>
                {
                    statisticService.UpdateUserActivity(activityRequest);

                    return Results.Ok();
                })
            .WithDescription("Обновление активности пользователя");

        activityGroup.MapGet("",
                (StatisticService statisticService) =>
                    Results.Ok(new ActivityResponse(statisticService.OnlineUsers.Count)))
            .WithDescription("Получить количество онлайн пользователей")
            .RequireAuthorization()
            .Produces<ActivityResponse>();
    }
}
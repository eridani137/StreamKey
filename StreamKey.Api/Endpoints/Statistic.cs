using Carter;
using StreamKey.Core.DTOs;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Entities;

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
                    if (hours <= 0 || count <= 0) return Results.BadRequest("Часов и количество записей должны быть больше 0");
                    
                    return Results.Json(await repository.GetTopViewedChannelsAsync(hours, count));
                })
            .WithSummary("Топ каналов")
            .RequireAuthorization();
        
        group.MapGet("/sessions/time-spent",
            async (int hours, UserSessionRepository repository) =>
            {
                if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                return Results.Ok(await repository.GetAverageTimeSpent(hours));
            })
            .WithSummary("Time Spent")
            .RequireAuthorization();

        group.MapGet("/channels/clicks",
                async (string channelName, int hours, ChannelClickRepository repository) =>
                {
                    if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                    return Results.Ok(await repository.GetChannelClicksCount(channelName, hours));
                })
            .WithSummary("");

        var activityGroup = app.MapGroup("/activity")
            .WithTags("Активность");

        activityGroup.MapPost("/update",
                (ActivityRequest activityRequest, StatisticService statisticService) =>
                {
                    statisticService.UpdateUserActivity(activityRequest);

                    return Results.Ok();
                })
            .WithSummary("Обновление активности пользователя");

        activityGroup.MapGet("",
                (StatisticService statisticService) =>
                    Results.Ok(new ActivityResponse(statisticService.OnlineUsers.Count)))
            .WithSummary("Получить число онлайн пользователей")
            .RequireAuthorization()
            .Produces<ActivityResponse>();
        
        activityGroup.MapPost("/channel/click",
                (ClickChannelDto dto, StatisticService service) =>
                {
                    service.ChannelActivityQueue.Enqueue(new ClickChannelEntity()
                    {
                        ChannelName = dto.ChannelName,
                        UserId = dto.UserId,
                        DateTime = DateTime.UtcNow
                    });
                })
            .WithSummary("Клик на канал");
    }
}
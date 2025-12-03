using Carter;
using StreamKey.Core.DTOs;
using StreamKey.Core.Hubs;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Types;

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
                    if (hours <= 0 || count <= 0)
                        return Results.BadRequest("Часов и количество записей должны быть больше 0");

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

        group.MapGet("/online",
                (StatisticService statisticService) =>
                    Results.Ok(new ActivityResponse(BrowserExtensionHub.Users.Count)))
            .WithSummary("Получить число онлайн пользователей")
            .RequireAuthorization()
            .Produces<ActivityResponse>();

        group.MapGet("/dau",
                async (DateOnly startDate, UserSessionRepository repository) =>
                    Results.Ok(await repository.GetUsersPerDayStatistic(startDate)))
            .WithSummary("Уникальные пользователи за день")
            .RequireAuthorization()
            .Produces<UsersPerTimeStatistic>();

        group.MapGet("/mau",
                async (DateOnly startDate, UserSessionRepository repository) =>
                    Results.Ok(await repository.GetUsersPerMonthStatistic(startDate)))
            .WithSummary("Уникальные пользователи за месяц")
            .RequireAuthorization()
            .Produces<UsersPerTimeStatistic>();

        group.MapGet("/channels/clicks",
                async (string channelName, int hours, ChannelClickRepository repository) =>
                {
                    if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                    return Results.Ok(await repository.GetChannelClicksCount(channelName, hours));
                })
            .Produces<ChannelClicksStatistic>()
            .WithSummary("Получение числа кликов на канал");
    }
}
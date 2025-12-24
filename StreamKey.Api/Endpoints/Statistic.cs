using Carter;
using StreamKey.Core.Common;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Statistic : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/statistic")
            .WithTags("Статистические данные");

        group.MapGet("/channels",
                async (int hours, int count, ViewStatisticRepository repository, CancellationToken cancellationToken) =>
                {
                    if (hours <= 0 || count <= 0)
                        return Results.BadRequest("Часов и количество записей должны быть больше 0");

                    return Results.Json(await repository.GetTopViewedChannelsAsync(hours, count, cancellationToken));
                })
            .WithSummary("Топ каналов")
            .RequireAuthorization();

        group.MapGet("/sessions/time-spent",
                async (int hours, UserSessionRepository repository, CancellationToken cancellationToken) =>
                {
                    if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                    return Results.Ok(await repository.GetAverageTimeSpent(hours, cancellationToken));
                })
            .WithSummary("Time Spent")
            .RequireAuthorization();

        group.MapGet("/online", (StatisticService statisticService) => Results.Ok(statisticService.GetOnline()))
            .WithSummary("Получить онлайн")
            .RequireAuthorization()
            .Produces<OnlineResponse>();

        group.MapGet("/dau",
                async (DateOnly startDate, UserSessionRepository repository, CancellationToken cancellationToken) =>
                    Results.Ok(await repository.GetUsersPerDayStatistic(startDate, cancellationToken)))
            .WithSummary("Уникальные пользователи за день")
            .RequireAuthorization()
            .Produces<UsersPerTimeStatistic>();

        group.MapGet("/mau",
                async (DateOnly startDate, UserSessionRepository repository, CancellationToken cancellationToken) =>
                    Results.Ok(await repository.GetUsersPerMonthStatistic(startDate, cancellationToken)))
            .WithSummary("Уникальные пользователи за месяц")
            .RequireAuthorization()
            .Produces<UsersPerTimeStatistic>();

        group.MapGet("/channels/clicks",
                async (string channelName, int hours, ChannelClickRepository repository,
                    CancellationToken cancellationToken) =>
                {
                    if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                    return Results.Ok(await repository.GetChannelClicksCount(channelName, hours, cancellationToken));
                })
            .Produces<ChannelClicksStatistic>()
            .WithSummary("Получение числа кликов на канал");
        
        group.MapGet("/buttons/clicks",
                async (string link, int hours, ButtonClickRepository repository,
                    CancellationToken cancellationToken) =>
                {
                    if (hours <= 0) return Results.BadRequest("Часов должно быть больше 0");

                    return Results.Ok(await repository.GetButtonClicksCount(link, hours, cancellationToken));
                })
            .Produces<ChannelClicksStatistic>()
            .WithSummary("Получение числа кликов на кнопку");
    }
}
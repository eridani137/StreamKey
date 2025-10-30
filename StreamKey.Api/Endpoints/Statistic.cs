using Carter;
using StreamKey.Core.DTOs;
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

        var activityGroup = app.MapGroup("/activity");

        activityGroup.MapPost("/update",
                (ActivityRequest activityRequest, ILogger<Statistic> logger) =>
                {
                    logger.LogInformation("{SessionId}, {UserId}", activityRequest.SessionId, activityRequest.UserId);
                })
            .WithDescription("Обновление активности пользователя");
    }
}
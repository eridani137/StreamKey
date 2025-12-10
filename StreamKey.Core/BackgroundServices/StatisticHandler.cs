using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Core.BackgroundServices;

public class StatisticHandler(
    StatisticService statisticService,
    IServiceScopeFactory scopeFactory,
    ILogger<StatisticHandler> logger)
    : BackgroundService
{
    private static readonly TimeSpan UserOfflineTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MinimumSessionDuration = TimeSpan.FromSeconds(30);

    private readonly PeriodicTaskRunner<StatisticHandler> _taskRunner = new(logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Сервис статистики запущен");

        await Task.WhenAll(
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), SaveViewStatistic, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), ct => RemoveOfflineUsers(false, ct), stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), SaveChannelClickStatistic, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(10), LogOnlineUsers, stoppingToken)
        );
    }

    private async Task SaveViewStatistic(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ViewStatisticRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            while (statisticService.ViewStatisticQueue.TryDequeue(out var entity))
            {
                try
                {
                    await repository.Add(entity, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при добавлении записи просмотра");
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики просмотров");
        }
    }

    private async Task RemoveOfflineUsers(bool shutdown, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var userIds = shutdown
                ? statisticService.OnlineUsers.Keys.ToList()
                : statisticService.OnlineUsers
                    .Where(kvp => kvp.Value.UpdatedAt < DateTimeOffset.UtcNow.Subtract(UserOfflineTimeout))
                    .Select(kvp => kvp.Key)
                    .ToList();

            foreach (var offlineUserId in userIds)
            {
                if (!statisticService.OnlineUsers.TryRemove(offlineUserId, out var offlineUser)) continue;
                var sessionDuration = offlineUser.UpdatedAt - offlineUser.StartedAt;
                if (sessionDuration < MinimumSessionDuration) continue;

                try
                {
                    await repository.Add(offlineUser, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при сохранении оффлайн пользователя {UserId}", offlineUserId);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении оффлайн пользователей");
        }
    }

    private async Task SaveChannelClickStatistic(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ChannelClickRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            while (statisticService.ChannelActivityQueue.TryDequeue(out var entity))
            {
                try
                {
                    await repository.Add(entity, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при добавлении записи клика на канал");
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики кликов на каналы");
        }
    }

    private Task LogOnlineUsers(CancellationToken cancellationToken)
    {
        logger.LogInformation("Текущий онлайн: {OnlineUsers}", statisticService.OnlineUsers.Count);
        return Task.CompletedTask;
    }
}
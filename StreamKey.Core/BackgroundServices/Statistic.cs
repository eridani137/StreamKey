using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Common;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Core.BackgroundServices;

public class Statistic(
    StatisticService statisticService,
    IServiceScopeFactory scopeFactory,
    ILogger<Statistic> logger)
    : BackgroundService
{
    private static readonly TimeSpan UserOfflineTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan MinimumSessionDuration = TimeSpan.FromSeconds(30);

    private readonly PeriodicTaskRunner<Statistic> _taskRunner = new(logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Сервис статистики запущен");

        await Task.WhenAll(
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), SaveViewStatistic, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), ct => SaveSessions(false, ct), stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), ct => SaveHubSessions(false, ct), stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), SaveChannelClickStatistic, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromMinutes(10), LogOnlineUsers, stoppingToken)
        );
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            SaveViewStatistic(CancellationToken.None),
            SaveSessions(true, CancellationToken.None),
            SaveHubSessions(true, CancellationToken.None),
            SaveChannelClickStatistic(CancellationToken.None)
        );

        await base.StopAsync(stoppingToken);
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

    private async Task SaveSessions(bool shutdown, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var sessionIds = shutdown
                ? statisticService.OnlineUsers.Keys.ToList()
                : statisticService.OnlineUsers
                    .Where(kvp => kvp.Value.UpdatedAt < DateTimeOffset.UtcNow.Subtract(UserOfflineTimeout))
                    .Select(kvp => kvp.Key)
                    .ToList();

            foreach (var sessionId in sessionIds)
            {
                if (!statisticService.OnlineUsers.TryRemove(sessionId, out var sessionEntity)) continue;
                var sessionDuration = sessionEntity.UpdatedAt - sessionEntity.StartedAt;
                if (sessionDuration < MinimumSessionDuration) continue;

                try
                {
                    await repository.Add(sessionEntity, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при сохранении сессии {SessionId}", sessionId);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении сессий");
        }
    }

    private async Task SaveHubSessions(bool shutdown, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var sessions = shutdown
                ? ConnectionRegistry.ActiveConnections.Values.Select(v => v.Map()).ToList()
                : ConnectionRegistry.DisconnectedConnections.Values
                    .Where(s => s.AccumulatedTime > MinimumSessionDuration).Select(v => v.Map()).ToList();

            ConnectionRegistry.DisconnectedConnections.Clear();

            foreach (var session in sessions)
            {
                try
                {
                    await repository.Add(session, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при сохранении сессии хаба {SessionId}", session.Id);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении оффлайн сессий хаба");
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

    private async Task LogOnlineUsers(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<StatisticService>();
        
        logger.LogInformation("Текущий онлайн: {@OnlineResponse}", service.GetOnline());
    }
}
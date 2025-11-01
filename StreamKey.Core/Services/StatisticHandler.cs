using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Services;

public class StatisticHandler(
    StatisticService statisticService,
    IServiceProvider serviceProvider,
    ILogger<StatisticHandler> logger)
    : IHostedService, IDisposable
{
    private static readonly TimeSpan SaveViewStatisticInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RemoveOfflineUsersInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan UserOfflineTimeout = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SaveClickChannelStatisticInterval = TimeSpan.FromMinutes(1);

    private Task? _savingViewStatistic;
    private Task? _removeOfflineUsers;
    private Task? _savingChannelClick;
    private CancellationTokenSource _stoppingCts = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Сервис статистики запущен");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _savingViewStatistic = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SaveViewStatisticInterval, _stoppingCts.Token);
                    await SaveViewStatistic();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в основном цикле сохранения статистики");
                }
            }
        }, _stoppingCts.Token);

        _removeOfflineUsers = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RemoveOfflineUsersInterval, _stoppingCts.Token);
                    await RemoveOfflineUsers(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в цикле удаления оффлайн пользователей");
                }
            }
        }, _stoppingCts.Token);

        _savingChannelClick = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SaveClickChannelStatisticInterval, _stoppingCts.Token);
                    await SaveChannelClickStatistic();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в цикле сохранения кликов на каналы");
                }
            }
        }, _stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveViewStatistic();
        await RemoveOfflineUsers(true);
        await SaveChannelClickStatistic();

        await _stoppingCts.CancelAsync();

        try
        {
            if (_savingViewStatistic is not null && _removeOfflineUsers is not null && _savingChannelClick is not null)
            {
                await Task.WhenAll(_savingViewStatistic, _removeOfflineUsers, _savingChannelClick)
                    .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (TimeoutException)
        {
            logger.LogWarning("Превышен таймаут при остановке фоновых задач");
        }

        logger.LogInformation("Сервис статистики остановлен");
    }


    private async Task SaveViewStatistic()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ViewStatisticRepository>();

            var processed = 0;

            while (statisticService.ViewStatisticQueue.TryDequeue(out var data))
            {
                try
                {
                    await repository.Add(data);
                    processed++;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при добавлении записи просмотра");
                }
            }

            await repository.Save();

            if (processed > 0)
            {
                logger.LogInformation("Сохранено {RecordsProcessedCount} записей просмотров", processed);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики просмотров");
        }
    }

    private async Task RemoveOfflineUsers(bool shutdown)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();

            List<string> userIds;
            if (!shutdown)
            {
                var offlineThreshold = DateTimeOffset.UtcNow.Subtract(UserOfflineTimeout);
                userIds = statisticService.OnlineUsers
                    .Where(kvp => kvp.Value.UpdatedAt < offlineThreshold)
                    .Select(s => s.Key)
                    .ToList();
            }
            else
            {
                userIds = statisticService.OnlineUsers.Keys.ToList();
            }
            
            await RemoveAndSaveUserSessions(userIds, repository);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении оффлайн пользователей");
        }
    }

    private async Task RemoveAndSaveUserSessions(List<string> userIds, UserSessionRepository repository)
    {
        var minimumSessionDuration = TimeSpan.FromSeconds(45);
        
        var processed = 0;
        
        foreach (var offlineUserId in userIds)
        {
            if (statisticService.OnlineUsers.TryRemove(offlineUserId, out var offlineUser))
            {
                var sessionDuration = offlineUser.UpdatedAt - offlineUser.StartedAt;
                
                if (sessionDuration >= minimumSessionDuration)
                {
                    await repository.Add(offlineUser);
                    processed++;
                }
            }
        }
        
        await repository.Save();
        
        if (processed > 0)
        {
            logger.LogInformation("Сохранено {OfflineUserSessions} сессий пользователей", processed);
        }
    }

    private async Task SaveChannelClickStatistic()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<ChannelClickRepository>();

            var processed = 0;

            while (statisticService.ChannelActivityQueue.TryDequeue(out var data))
            {
                try
                {
                    await repository.Add(data);
                    processed++;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при добавлении записи клика на канал");
                }
            }
            
            await repository.Save();
            
            if (processed > 0)
            {
                logger.LogInformation("Сохранено {RecordsProcessedCount} записей кликов на каналы", processed);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики кликов на каналы");
        }
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();
        _savingViewStatistic?.Dispose();
        _removeOfflineUsers?.Dispose();
        _savingChannelClick?.Dispose();
        _stoppingCts.Dispose();
    }
}
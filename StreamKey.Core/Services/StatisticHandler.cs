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
    private static readonly TimeSpan SaveStatisticInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RemoveOfflineUsersInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan UserOfflineTimeout = TimeSpan.FromMinutes(1);

    private Task? _savingStatisticTask;
    private Task? _removeOfflineUsers;
    private CancellationTokenSource _stoppingCts = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Сервис статистики запущен");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _savingStatisticTask = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), _stoppingCts.Token);

            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SaveStatisticInterval, _stoppingCts.Token);
                    await SaveStatistic();
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
            await Task.Delay(TimeSpan.FromSeconds(5), _stoppingCts.Token);

            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(RemoveOfflineUsersInterval, _stoppingCts.Token);
                    await RemoveOfflineUsers();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в основном цикле с оффлайн пользователей");
                }
            }
        }, _stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveStatistic();

        await _stoppingCts.CancelAsync();

        try
        {
            if (_savingStatisticTask is not null && _removeOfflineUsers is not null)
            {
                await Task.WhenAll(_savingStatisticTask, _removeOfflineUsers)
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


    private async Task SaveStatistic()
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
                    logger.LogError(e, "Ошибка при добавлении статистической записи");
                }
            }

            await repository.Save();

            if (processed > 0)
            {
                logger.LogInformation("Сохранено {RecordsProcessedCount} статистических записей просмотров", processed);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики");
        }
    }

    private async Task RemoveOfflineUsers()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();
            
            var processed = 0;
            
            var offlineThreshold = DateTimeOffset.UtcNow.Subtract(UserOfflineTimeout);
            var minimumSessionDuration = TimeSpan.FromSeconds(45);

            var offlineUsersIds = statisticService.OnlineUsers
                .Where(kvp => kvp.Value.UpdatedAt < offlineThreshold)
                .Select(s => s.Key)
                .ToList();

            foreach (var offlineUserId in offlineUsersIds)
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
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении оффлайн пользователей");
        }
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();
        _savingStatisticTask?.Dispose();
        _removeOfflineUsers?.Dispose();
        _stoppingCts.Dispose();
    }
}
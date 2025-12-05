using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Hubs;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.BackgroundServices;

public class StatisticHandler(
    StatisticService statisticService,
    IServiceProvider serviceProvider,
    ILogger<StatisticHandler> logger)
    : IHostedService, IDisposable
{
    private static readonly TimeSpan StoppingTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan SaveViewStatisticInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RemoveOfflineUsersInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SaveClickChannelStatisticInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LoggingOnlineInterval = TimeSpan.FromMinutes(10);

    private Task? _savingViewStatistic;
    private Task? _removeOfflineUsers;
    private Task? _savingChannelClick;
    private Task? _loggingOnline;

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
                    await SaveViewStatistic(cancellationToken);
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
                    await RemoveOfflineUsers(false, cancellationToken);
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
                    await SaveChannelClickStatistic(cancellationToken);
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

        _loggingOnline = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                await Task.Delay(LoggingOnlineInterval, _stoppingCts.Token);
                logger.LogInformation("Текущий онлайн: {OnlineUsers}", BrowserExtensionHub.Users.Count);
            }
        }, _stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveViewStatistic(cancellationToken);
        await RemoveOfflineUsers(true, cancellationToken);
        await SaveChannelClickStatistic(cancellationToken);

        await _stoppingCts.CancelAsync();

        try
        {
            if (_savingViewStatistic is not null && _removeOfflineUsers is not null && _savingChannelClick is not null)
            {
                await Task.WhenAll(_savingViewStatistic, _removeOfflineUsers, _savingChannelClick)
                    .WaitAsync(StoppingTimeout, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Превышен таймаут при остановке фоновых задач");
        }

        logger.LogInformation("Сервис статистики остановлен");
    }


    private async Task SaveViewStatistic(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
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

    private async Task RemoveOfflineUsers(bool isShutdown, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<UserSessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var users = isShutdown
                ? BrowserExtensionHub.Users.Values.Select(v => v.Map()).ToList()
                : BrowserExtensionHub.DisconnectedUsers.Values.Select(v => v.Map()).ToList();

            BrowserExtensionHub.DisconnectedUsers.Clear();

            await RemoveAndSaveDisconnectedUserSessions(users, repository, unitOfWork, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при удалении оффлайн пользователей");
        }
    }

    private async Task RemoveAndSaveDisconnectedUserSessions(List<UserSessionEntity> entities,
        UserSessionRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        foreach (var sessionEntity in entities)
        {
            await repository.Add(sessionEntity, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (entities.Count != 0)
        {
            logger.LogDebug("Сохранено {DisconnectedUserSessions} сессий отключившихся пользователей",
                entities.Count);
        }
    }

    private async Task SaveChannelClickStatistic(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
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

    public void Dispose()
    {
        _stoppingCts.Cancel();

        _savingViewStatistic?.Dispose();
        _removeOfflineUsers?.Dispose();
        _savingChannelClick?.Dispose();
        _loggingOnline?.Dispose();

        _stoppingCts.Dispose();
    }
}
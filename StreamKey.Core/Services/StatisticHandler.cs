using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Core.Services;

public class StatisticHandler(
    StatisticService statisticService,
    IServiceProvider serviceProvider,
    ILogger<StatisticHandler> logger)
    : IHostedService, IDisposable
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(1);

    private Task? _executingTask;
    private CancellationTokenSource _stoppingCts = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Сервис статистики запущен");

        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _executingTask = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), _stoppingCts.Token);

            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(SaveInterval, _stoppingCts.Token);
                    await SaveStatistic();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в основном цикле сервиса статистики");
                }
            }
        }, _stoppingCts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveStatistic();

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            await _stoppingCts.CancelAsync();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }

        logger.LogInformation("Сервис статистики остановлен");
    }

    private async Task SaveStatistic()
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<StatisticRepository>();

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

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _executingTask?.Dispose();
        _stoppingCts?.Dispose();
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Core.Services;

public class StatisticHandler(
    StatisticService statisticService,
    IServiceProvider serviceProvider,
    ILogger<ChannelHandler> logger)
    : IHostedService
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(1);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        
        logger.LogInformation("Сервис статистики запущен");

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(SaveInterval, cancellationToken);
            
            await SaveStatistic();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await SaveStatistic();
        
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
                    logger.LogError(e, "Ошибка при добавление статистической записи");
                }
            }

            await repository.Save();
            
            logger.LogInformation("Сохранено {RecordsProcessedCount} статистических записей", processed);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении статистики");
        }
    }
}
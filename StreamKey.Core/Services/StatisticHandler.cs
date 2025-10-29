using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Repositories;

namespace StreamKey.Core.Services;

public class StatisticHandler(
    StatisticService service,
    IServiceProvider serviceProvider,
    ILogger<ChannelHandler> logger)
    : BackgroundService
{
    private static readonly TimeSpan SaveInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<StatisticRepository>();
                
                while (service.ViewStatisticQueue.TryDequeue(out var data))
                {
                    try
                    {
                        await repository.Add(data);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Ошибка при добавление статистической записи");
                    }
                }

                await repository.Save();
                
                await Task.Delay(SaveInterval, stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при сохранении статистики");
            }
        }
    }
}
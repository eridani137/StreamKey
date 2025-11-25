using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreamKey.Core.Services;

public class RestartService(IHostApplicationLifetime appLifetime, ILogger<RestartService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var restartTime = new DateTime(now.Year, now.Month, now.Day, 7, 0, 0);
            
            if (now.TimeOfDay >= new TimeSpan(7, 0, 0))
            {
                restartTime = restartTime.AddDays(1);
            }
            
            var delay = restartTime - now;

            logger.LogInformation(
                "RestartService: Текущее время: {Now}, Следующий перезапуск в: {RestartTime}, Задержка: {DelayTotalMinutes:F0} минут",
                now,
                restartTime,
                delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation("RestartService: Отменен");
                break;
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("RestartService: Плановый перезапуск сервиса в {Time}", DateTime.Now);
                appLifetime.StopApplication();
                break;
            }
        }
    }
}

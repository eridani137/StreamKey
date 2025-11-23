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
            var next4Am = new DateTime(now.Year, now.Month, now.Day, 4, 0, 0);
            if (now > next4Am)
            {
                next4Am = next4Am.AddDays(1);
            }
            var delay = next4Am - now;

            logger.LogInformation("RestartService: Ждем до {DateTime} ({DelayTotalMinutes} минут)", next4Am, delay.TotalMinutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            logger.LogInformation("RestartService: Плановый перезапуск сервиса...");
            appLifetime.StopApplication();
        }
    }
}
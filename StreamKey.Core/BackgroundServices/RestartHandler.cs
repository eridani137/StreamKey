using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.BackgroundServices;

public class RestartHandler(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime appLifetime,
    ILogger<RestartHandler> logger)
    : BackgroundService
{
    private readonly TimeSpan _time = new(1, 0, 0);
    private readonly TimeSpan _checkDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRestartRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var lastRestartEntity = await repository.GetLastRestart();

            var now = DateTime.UtcNow;
            var timeToday = new DateTime(now.Year, now.Month, now.Day, _time.Hours, _time.Minutes, _time.Seconds);

            if (now >= timeToday)
            {
                if (lastRestartEntity == null || lastRestartEntity.DateTime.Date < now.Date)
                {
                    logger.LogInformation("Плановый перезапуск сервиса");

                    await repository.Add(new RestartEntity
                    {
                        DateTime = now
                    });
                    await unitOfWork.SaveChangesAsync(stoppingToken);

                    appLifetime.StopApplication();
                    break;
                }
            }

            try
            {
                await Task.Delay(_checkDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
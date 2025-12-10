using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.BackgroundServices;

public class RestartHandler(
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime appLifetime,
    ILogger<RestartHandler> logger)
    : BackgroundService
{
    private readonly TimeSpan _restartTime = new(1, 0, 0); // 01:00
    
    private readonly PeriodicTaskRunner<RestartHandler> _taskRunner = new(logger);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _taskRunner.RunAsync(TimeSpan.FromMinutes(1), CheckAndRestartIfNeeded, stoppingToken);
    }

    private async Task CheckAndRestartIfNeeded(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRestartRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var lastRestart = await repository.GetLastRestart(cancellationToken);

        var now = DateTime.UtcNow;
        var restartToday = new DateTime(now.Year, now.Month, now.Day, _restartTime.Hours, _restartTime.Minutes, _restartTime.Seconds);

        if (now < restartToday)
            return; // ещё не наступило время

        if (lastRestart != null && lastRestart.DateTime.Date >= now.Date)
            return; // уже был сегодня

        logger.LogInformation("Плановый перезапуск приложения");

        await repository.Add(new RestartEntity { DateTime = now }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        appLifetime.StopApplication();
    }
}
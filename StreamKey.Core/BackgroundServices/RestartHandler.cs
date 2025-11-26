using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.BackgroundServices;

public class RestartHandler(
    IRestartRepository repository,
    IUnitOfWork unitOfWork,
    IHostApplicationLifetime appLifetime,
    ILogger<RestartHandler> logger)
    : BackgroundService
{
    private readonly TimeSpan _time = new(1, 0, 0);
    private readonly TimeSpan _checkDelay = TimeSpan.FromMinutes(1);
    private RestartEntity? _lastRestartEntity;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _lastRestartEntity = await repository.GetLastRestart();
            
            var now = DateTime.UtcNow;
            var timeToday = new DateTime(now.Year, now.Month, now.Day, _time.Hours, _time.Minutes, _time.Seconds);
            
            if (now >= timeToday)
            {
                if (_lastRestartEntity is null || _lastRestartEntity.DateTime.Date < now.Date)
                {
                    logger.LogInformation("Плановый перезапуск");

                    await repository.Add(new RestartEntity()
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

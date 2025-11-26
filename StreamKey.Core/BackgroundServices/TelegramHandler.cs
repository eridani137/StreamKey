using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StreamKey.Core.BackgroundServices;

public class TelegramHandler(ILogger<RestartHandler> logger)
    : BackgroundService
{
    private readonly TimeSpan _time = new(0, 0, 0);
    private readonly TimeSpan _checkDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var timeToday = new DateTime(now.Year, now.Month, now.Day, _time.Hours, _time.Minutes, _time.Seconds);

            if (now >= timeToday)
            {
                timeToday = timeToday.AddDays(1);
            }

            if (now >= timeToday)
            {
                // TODO
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
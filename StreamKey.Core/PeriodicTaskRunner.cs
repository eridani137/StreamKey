using Microsoft.Extensions.Logging;

namespace StreamKey.Core;

public class PeriodicTaskRunner<T>(ILogger<T> logger)
{
    public async Task RunAsync(TimeSpan interval, Func<CancellationToken, Task> action, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await action(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при выполнении периодической задачи");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
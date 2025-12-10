using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Core.BackgroundServices;

public class TelegramHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramHandler> logger)
    : BackgroundService
{
    private readonly PeriodicTaskRunner<TelegramHandler> _taskRunner = new(logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Телеграм сервис запущен");

        await Task.WhenAll(
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), CheckOldUsers, stoppingToken)
        );
    }
    
    private async Task CheckOldUsers(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
        var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var users = await repository.GetOldestUpdatedUsers(10, cancellationToken);

        foreach (var user in users)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var response = await service.GetChatMember(user.TelegramId, cancellationToken);
                if (response is null) continue;

                var isChatMember = response.IsChatMember();
                if (user.IsChatMember != isChatMember)
                {
                    user.IsChatMember = isChatMember;
                }

                user.UpdatedAt = now;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при проверке пользователя {TelegramId}", user.TelegramId);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Core.BackgroundServices;

public class TelegramHandler(
    IServiceProvider serviceProvider,
    ILogger<TelegramHandler> logger)
    : BackgroundService
{
    private readonly TimeSpan _checkDelay = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            using var scope = serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var processedUsersCount = 0;
            var users = await repository.GetOldestUpdatedUsers(30, stoppingToken);
            foreach (var user in users)
            {
                if (stoppingToken.IsCancellationRequested) break;
                
                var response = await service.GetChatMember(user.TelegramId, stoppingToken);
                if (response is null) continue;
                
                var isChatMember = response.IsChatMember();
                if (user.IsChatMember != isChatMember)
                {
                    user.IsChatMember = isChatMember;
                    processedUsersCount++;
                }

                user.UpdatedAt = now;

                await Task.Delay(1000, stoppingToken);
            }

            await unitOfWork.SaveChangesAsync(stoppingToken);

            if (processedUsersCount > 0)
            {
                logger.LogInformation("Обработано {UsersCount} тг пользователей, изменился статус подписки у {ProcessedUsersCount} пользователя", users.Count, processedUsersCount);
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
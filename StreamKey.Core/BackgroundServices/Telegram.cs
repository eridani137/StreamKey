using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Common;
using StreamKey.Core.Extensions;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.Hubs;

namespace StreamKey.Core.BackgroundServices;

public class Telegram(
    IServiceScopeFactory scopeFactory,
    ILogger<Telegram> logger)
    : BackgroundService
{
    private readonly PeriodicTaskRunner<Telegram> _taskRunner = new(logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Телеграм сервис запущен");

        await Task.WhenAll(
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), CheckOldUsers, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromSeconds(15), SaveNewTelegramUsers, stoppingToken)
        );
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
            SaveNewTelegramUsers(CancellationToken.None)
        );

        await base.StopAsync(cancellationToken);
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
                if (response is null)
                {
                    user.IsChatMember = false;
                }
                else
                {
                    var isChatMember = response.IsChatMember();
                    if (user.IsChatMember != isChatMember)
                    {
                        user.IsChatMember = isChatMember;
                    }
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

    private async Task SaveNewTelegramUsers(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var extensionHub = scope.ServiceProvider
                .GetRequiredService<IHubContext<BrowserExtensionHub, IBrowserExtensionHub>>();

            while (ConnectionRegistry.NewTelegramUsers.TryDequeue(out var dto))
            {
                try
                {
                    var user = await repository.GetByTelegramId(dto.Id, cancellationToken);

                    var chatMember = await service.GetChatMember(dto.Id, cancellationToken);
                    var isChatMember = chatMember?.IsChatMember() ?? false;
                    
                    if (user is null)
                    {
                        user = dto.Map();
                        user.UpdateUserProperties(dto, isChatMember);
                        await repository.Add(user, cancellationToken);
                    }
                    else
                    {
                        user.UpdateUserProperties(dto, isChatMember);
                        repository.Update(user);
                    }
                    
                    if (ConnectionRegistry.GetConnectionIdBySessionId(dto.SessionId) is { } connectionId)
                    {
                        await extensionHub.Clients.Client(connectionId)
                            .ReloadUserData(dto.MapUserDto(user.IsChatMember));
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Ошибка при добавлении нового необработанного пользователя");
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка при сохранении новых необработанных пользователей");
        }
    }
}
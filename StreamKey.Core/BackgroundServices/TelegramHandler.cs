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
            _taskRunner.RunAsync(TimeSpan.FromMinutes(1), CheckOldUsers, stoppingToken),
            _taskRunner.RunAsync(TimeSpan.FromSeconds(5), SaveNewTelegramUsers, stoppingToken)
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

                    if (user is null)
                    {
                        user = dto.Map();
                        await repository.Add(user, cancellationToken);
                    }

                    var chatMember = await service.GetChatMember(dto.Id, cancellationToken);
                    if (chatMember is null) continue;

                    user.FirstName = dto.FirstName;
                    user.Username = dto.Username;
                    user.AuthDate = dto.AuthDate;
                    user.PhotoUrl = dto.PhotoUrl;
                    user.Hash = dto.Hash;
                    user.IsChatMember = chatMember.IsChatMember();
                    user.AuthorizedAt = DateTime.UtcNow;

                    repository.Update(user);
                    
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
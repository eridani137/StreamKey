using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Extensions;
using StreamKey.Core.Hubs;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Core.BackgroundServices;

public class TelegramHandler(
    IServiceProvider serviceProvider,
    ILogger<TelegramHandler> logger)
    : IHostedService, IDisposable
{
    private static readonly TimeSpan StoppingTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan CheckOldUsersInterval = TimeSpan.FromMinutes(1);

    public static ConcurrentQueue<TelegramAuthDtoWithSessionId> NewUsers { get; } = new();

    private Task? _checkOldUsers;
    private Task? _saveNewUsers;

    private CancellationTokenSource _stoppingCts = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Телеграм сервис запущен");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _checkOldUsers = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(CheckOldUsersInterval, _stoppingCts.Token);
                    await CheckOldUsers();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в основном цикле проверки старых пользователей");
                }
            }
        }, _stoppingCts.Token);

        _saveNewUsers = Task.Run(async () =>
        {
            while (!_stoppingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await SaveNewUsers(_stoppingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка в основном цикле проверки новых пользователей");
                }
            }
        }, _stoppingCts.Token);

        return Task.CompletedTask;
    }

    private async Task SaveNewUsers(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
            var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var extensionHub = scope.ServiceProvider
                .GetRequiredService<IHubContext<BrowserExtensionHub, IBrowserExtensionHub>>();

            while (NewUsers.TryDequeue(out var dto))
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
                    
                    if (BrowserExtensionHub.GetConnectionIdBySessionId(dto.SessionId) is { } connectionId)
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _stoppingCts.CancelAsync();
        
        await SaveNewUsers(cancellationToken);

        try
        {
            if (_checkOldUsers is not null && _saveNewUsers is not null)
            {
                await Task.WhenAll(_checkOldUsers, _saveNewUsers)
                    .WaitAsync(StoppingTimeout, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Превышен таймаут при остановке фоновых задач");
        }

        logger.LogInformation("Телеграм сервис остановлен");
    }

    private async Task CheckOldUsers()
    {
        var now = DateTime.UtcNow;

        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITelegramService>();
        var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var users = await repository.GetOldestUpdatedUsers(10, _stoppingCts.Token);
        foreach (var user in users)
        {
            if (_stoppingCts.IsCancellationRequested) break;

            var response = await service.GetChatMember(user.TelegramId, _stoppingCts.Token);
            if (response is null) continue;

            var isChatMember = response.IsChatMember();
            if (user.IsChatMember != isChatMember)
            {
                user.IsChatMember = isChatMember;
            }

            user.UpdatedAt = now;
        }

        await unitOfWork.SaveChangesAsync(_stoppingCts.Token);
    }

    public void Dispose()
    {
        _stoppingCts.Cancel();

        _checkOldUsers?.Dispose();
        _saveNewUsers?.Dispose();

        _stoppingCts.Dispose();
    }
}
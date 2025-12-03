using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Core.Types;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.Hubs;

public class BrowserExtensionHub
    : Hub<IBrowserExtensionHub>
{
    public static ConcurrentDictionary<string, UserSession> Users { get; } = new();

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _registrationTimeouts = new();
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan AddingTime = TimeSpan.FromMinutes(1);
    
    public static ConcurrentDictionary<string, UserSession> DisconnectedUsers { get; } = new();

    public override async Task OnConnectedAsync()
    {
        var context = Context;
        var connectionId = Context.ConnectionId;

        await Clients.Caller.RequestUserData();

        var cancellationTokenSource = new CancellationTokenSource();
        _registrationTimeouts[connectionId] = cancellationTokenSource;

        _ = Task.Delay(RegistrationTimeout, cancellationTokenSource.Token)
            .ContinueWith(task =>
            {
                if (task.IsCanceled) return;

                if (Users.ContainsKey(connectionId)) return;

                // logger.LogWarning(
                //     "Пользователь не предоставил свои данные в течение {Timeout} секунд. Отключение: {ConnectionId}",
                //     RegistrationTimeout.TotalSeconds, connectionId);

                context.Abort();
                _registrationTimeouts.TryRemove(connectionId, out _);
            }, cancellationTokenSource.Token);

        await base.OnConnectedAsync();
    }

    public Task EntranceUserData(UserData userData)
    {
        var connectionId = Context.ConnectionId;

        var session = new UserSession()
        {
            SessionId = userData.SessionId,
            StartedAt = DateTimeOffset.UtcNow
        };

        if (!Users.TryAdd(connectionId, session))
        {
            // logger.LogWarning("Вход пользователя не удался: {@UserData}", userData);
            Context.Abort();
            return Task.CompletedTask;
        }

        CancelRegistrationTimeout(connectionId);
        return Task.CompletedTask;
    }

    private void CancelRegistrationTimeout(string connectionId)
    {
        if (!_registrationTimeouts.TryRemove(connectionId, out var cancellationTokenSource)) return;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }

    public Task UpdateActivity(ActivityRequest activityRequest)
    {
        if (!Users.TryGetValue(Context.ConnectionId, out var session)) return Task.CompletedTask;

        var now = DateTimeOffset.UtcNow;
        
        session.UserId ??= activityRequest.UserId;

        if (session.UpdatedAt == DateTimeOffset.MinValue || session.UpdatedAt >= now.Add(-AddingTime))
        {
            if (session.UpdatedAt == DateTimeOffset.MinValue)
            {
                session.StartedAt = now;
            }
            session.UpdatedAt = now;
            session.AccumulatedTime += AddingTime;
        }

        return Task.CompletedTask;
    }

    public Task ClickChannel(ClickChannelDto dto, [FromServices] StatisticService service)
    {
        service.ChannelActivityQueue.Enqueue(new ClickChannelEntity()
        {
            ChannelName = dto.ChannelName,
            UserId = dto.UserId,
            DateTime = DateTime.UtcNow
        });
        
        return Task.CompletedTask;
    }

    public async Task<TelegramUserDto?> GetTelegramUser(TelegramUserRequest request, [FromServices] ITelegramUserRepository repository)
    {
        var user = await repository.GetByTelegramIdNotTracked(request.UserId);
        if (user is null) return null;
        
        if (!string.Equals(request.UserHash, user.Hash, StringComparison.Ordinal))
        {
            return null;
        }

        return user.MapUserDto();
    }

    public async Task<List<ChannelDto>> GetChannels([FromServices] IChannelService service)
    {
        var channels = await service.GetChannels();
        return channels.Map();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        CancelRegistrationTimeout(connectionId);

        if (Users.TryRemove(connectionId, out var userSession))
        {
            if (userSession.UserId is not null)
            {
                DisconnectedUsers.TryAdd(connectionId, userSession);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}
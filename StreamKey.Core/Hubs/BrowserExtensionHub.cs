using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Types;

namespace StreamKey.Core.Hubs;

public class BrowserExtensionHub
    : Hub<IBrowserExtensionHub>
{
    public static ConcurrentDictionary<string, UserSession> Users { get; } = new();

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _registrationTimeouts = new();
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan AddingTime = TimeSpan.FromMinutes(1);

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

        if (session.UpdatedAt >= now.Add(-AddingTime))
        {
            session.UpdatedAt = now;
            session.AccumulatedTime += AddingTime;
        }

        return Task.CompletedTask;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        CancelRegistrationTimeout(connectionId);

        Users.TryRemove(connectionId, out _);

        await base.OnDisconnectedAsync(exception);
    }
}
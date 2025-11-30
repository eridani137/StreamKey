using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Types;

namespace StreamKey.Core.Hubs;

public class BrowserExtensionHub(
    ILogger<BrowserExtensionHub> logger)
    : Hub<IBrowserExtensionHub>
{
    public static ConcurrentDictionary<string, UserData> Users { get; } = new();

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _registrationTimeouts = new();
    private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromSeconds(7);

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

                logger.LogWarning(
                    "Пользователь не предоставил свои данные в течение {Timeout} секунд. Отключение: {ConnectionId}",
                    RegistrationTimeout.TotalSeconds, connectionId);

                context.Abort();
                _registrationTimeouts.TryRemove(connectionId, out _);
            }, cancellationTokenSource.Token);

        await base.OnConnectedAsync();
    }

    public Task EntranceUserData(UserData? userData)
    {
        if (userData is null)
        {
            logger.LogWarning("Получены невалидные данные пользователя: {@UserData}", userData);
            Context.Abort();
            return Task.CompletedTask;
        }

        var connectionId = Context.ConnectionId;

        if (!Users.TryAdd(connectionId, userData))
        {
            logger.LogWarning("Вход пользователя не удался: {@UserData}", userData);
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        CancelRegistrationTimeout(connectionId);

        Users.TryRemove(connectionId, out _);

        await base.OnDisconnectedAsync(exception);
    }
}
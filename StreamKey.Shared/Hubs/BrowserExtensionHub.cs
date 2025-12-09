using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Types;

namespace StreamKey.Shared.Hubs;

public class BrowserExtensionHub(
    IConnectionStore connectionStore,
    IStatisticStore statisticStore,
    ILogger<BrowserExtensionHub> logger)
    : Hub<IBrowserExtensionHub>
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> RegistrationTimeouts = new();

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MinimumSessionTime = TimeSpan.FromMinutes(1);

    public override async Task OnConnectedAsync()
    {
        var context = Context;
        var connectionId = context.ConnectionId;

        var cts = new CancellationTokenSource();
        RegistrationTimeouts.TryAdd(connectionId, cts);

        logger.LogInformation("Новое подключение: {ConnectionId}", connectionId);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ConnectionTimeout, cts.Token);

                var session = await connectionStore.GetSessionAsync(connectionId);
                if (session is null)
                {
                    logger.LogWarning("Таймаут регистрации пользователя: {ConnectionId}", connectionId);
                    context.Abort();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }, cts.Token);

        await Clients.Caller.RequestUserData();

        await base.OnConnectedAsync();
    }

    public async Task EntranceUserData(UserData userData)
    {
        var connectionId = Context.ConnectionId;

        var session = new UserSession()
        {
            SessionId = userData.SessionId,
            StartedAt = DateTimeOffset.UtcNow
        };

        await connectionStore.AddConnectionAsync(connectionId, session);

        CancelRegistrationTimeout(connectionId);

        logger.LogInformation("Пользователь предоставил данные: {@UserData}", userData);
    }

    private void CancelRegistrationTimeout(string connectionId)
    {
        if (!RegistrationTimeouts.TryRemove(connectionId, out var cts)) return;

        cts.Cancel();
        cts.Dispose();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;

        CancelRegistrationTimeout(connectionId);

        var session = await connectionStore.GetSessionAsync(connectionId);
        if (session is not null)
        {
            if (session.UserId is not null)
            {
                await connectionStore.MoveToDisconnectedAsync(connectionId, session);
            }
            else
            {
                await connectionStore.RemoveConnectionAsync(connectionId);
            }

            logger.LogInformation("Пользователь отключен: {@Session}", session);
        }
        else
        {
            logger.LogInformation("Пользователь отключен с пустой сессией: {ConnectionId}", connectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateActivity(ActivityRequest activityRequest)
    {
        var connectionId = Context.ConnectionId;

        var session = await connectionStore.GetSessionAsync(connectionId);
        if (session == null) return;

        var now = DateTimeOffset.UtcNow;

        session.UserId ??= activityRequest.UserId;

        if (session.UpdatedAt == DateTimeOffset.MinValue || session.UpdatedAt >= now.Add(-MinimumSessionTime))
        {
            if (session.UpdatedAt == DateTimeOffset.MinValue)
            {
                session.StartedAt = now;
            }

            session.UpdatedAt = now;
            session.AccumulatedTime += MinimumSessionTime;

            await connectionStore.AddConnectionAsync(connectionId, session);
        }
    }

    public async Task ClickChannel(ClickChannel dto)
    {
        await statisticStore.SaveClickAsync(dto);
    }
    
    // public async Task<TelegramUserDto?> GetTelegramUser(TelegramUserRequest request,
    //     [FromServices] ITelegramUserRepository repository)
    // {
    //     var user = await repository.GetByTelegramIdNotTracked(request.UserId, Context.ConnectionAborted);
    //     if (user is null) return null;
    //
    //     if (!string.Equals(request.UserHash, user.Hash, StringComparison.Ordinal))
    //     {
    //         return null;
    //     }
    //
    //     return user.MapUserDto();
    // }
    //
    // public async Task<List<ChannelDto>> GetChannels([FromServices] IChannelService service)
    // {
    //     var channels = await service.GetChannels(Context.ConnectionAborted);
    //     return channels.Map();
    // }
    //
    // public async Task CheckMember(CheckMemberRequest request,
    //     [FromServices] ITelegramUserRepository repository,
    //     [FromServices] ITelegramService service,
    //     [FromServices] IUnitOfWork unitOfWork)
    // {
    //     var user = await repository.GetByTelegramId(request.UserId, Context.ConnectionAborted);
    //     if (user is null) return;
    //
    //     var getChatMemberResponse = await service.GetChatMember(request.UserId, Context.ConnectionAborted);
    //     if (getChatMemberResponse is null) return;
    //
    //     var isChatMember = getChatMemberResponse.IsChatMember();
    //     if (user.IsChatMember != isChatMember)
    //     {
    //         user.IsChatMember = isChatMember;
    //         user.UpdatedAt = DateTime.UtcNow;
    //
    //         repository.Update(user);
    //         await unitOfWork.SaveChangesAsync(Context.ConnectionAborted);
    //     }
    //
    //     await Clients.Caller.ReloadUserData(user.MapUserDto());
    // }
}
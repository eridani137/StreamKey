using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Extensions;
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
    public static ConcurrentDictionary<string, UserSession> DisconnectedUsers { get; } = new();

    private static readonly ConcurrentDictionary<string, CancellationTokenSource> RegistrationTimeouts = new();

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MinimumSessionTime = TimeSpan.FromMinutes(1);

    public override async Task OnConnectedAsync()
    {
        var context = Context;
        var connectionId = context.ConnectionId;

        var cts = new CancellationTokenSource();
        RegistrationTimeouts.TryAdd(connectionId, cts);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ConnectionTimeout, cts.Token);

                if (!Users.ContainsKey(connectionId))
                {
                    // logger.LogWarning("Таймаут регистрации пользователя: {ConnectionId}", connectionId);
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

        // logger.LogInformation("Пользователь предоставил данные: {@UserData}", userData);

        CancelRegistrationTimeout(connectionId);

        return Task.CompletedTask;
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

        if (Users.TryRemove(connectionId, out var userSession))
        {
            if (userSession.UserId is not null)
            {
                DisconnectedUsers.TryAdd(connectionId, userSession);
            }

            // logger.LogWarning("Пользователь отключен: {@Session}", userSession);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public Task UpdateActivity(ActivityRequest activityRequest)
    {
        if (!Users.TryGetValue(Context.ConnectionId, out var session)) return Task.CompletedTask;

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

    public async Task<TelegramUserDto?> GetTelegramUser(TelegramUserRequest request,
        [FromServices] ITelegramUserRepository repository, CancellationToken cancellationToken)
    {
        var user = await repository.GetByTelegramIdNotTracked(request.UserId, cancellationToken);
        if (user is null) return null;

        if (!string.Equals(request.UserHash, user.Hash, StringComparison.Ordinal))
        {
            return null;
        }

        return user.MapUserDto();
    }

    public async Task<List<ChannelDto>> GetChannels([FromServices] IChannelService service, CancellationToken cancellationToken)
    {
        var channels = await service.GetChannels(cancellationToken);
        return channels.Map();
    }

    public async Task CheckMember(CheckMemberRequest request,
        [FromServices] ITelegramUserRepository repository,
        [FromServices] ITelegramService service,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var user = await repository.GetByTelegramId(request.UserId, cancellationToken);
        if (user is null) return;

        var getChatMemberResponse = await service.GetChatMember(request.UserId, cancellationToken);
        if (getChatMemberResponse is null) return;

        var isChatMember = getChatMemberResponse.IsChatMember();
        if (user.IsChatMember != isChatMember)
        {
            user.IsChatMember = isChatMember;
            user.UpdatedAt = DateTime.UtcNow;

            repository.Update(user);
            await unitOfWork.SaveChangesAsync();
        }

        await Clients.Caller.ReloadUserData(user.MapUserDto());
    }
}
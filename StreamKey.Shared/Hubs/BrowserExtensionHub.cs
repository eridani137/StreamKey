using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Types;

namespace StreamKey.Shared.Hubs;

public class BrowserExtensionHub(
    INatsConnection nats,
    ILogger<BrowserExtensionHub> logger)
    : Hub<IBrowserExtensionHub>
{
    private readonly MessagePackNatsSerializer<UserSessionMessage> _serializer = new();
    
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> RegistrationTimeouts = new();

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var cts = new CancellationTokenSource();
        RegistrationTimeouts[connectionId] = cts;

        logger.LogInformation("Новое подключение: {ConnectionId}", connectionId);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ConnectionTimeout, cts.Token);
                logger.LogWarning("Таймаут регистрации пользователя: {ConnectionId}", connectionId);
                Context.Abort();
            }
            catch (TaskCanceledException) { }
        }, cts.Token);

        await Clients.Caller.RequestUserData();
        await base.OnConnectedAsync();
    }
    
    
    public async Task EntranceUserData(UserData userData)
    {
        var connectionId = Context.ConnectionId;
        var now = DateTimeOffset.UtcNow;

        var sessionMessage = new UserSessionMessage
        {
            ConnectionId = connectionId,
            Session = new UserSession
            {
                SessionId = userData.SessionId,
                StartedAt = now
            }
        };

        await nats.PublishAsync(NatsKeys.Connection, sessionMessage, serializer: _serializer);
        CancelRegistrationTimeout(connectionId);

        logger.LogInformation("Пользователь зарегистрирован: {@UserData}", userData);
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

        var message = new UserSessionMessage()
        {
            ConnectionId = connectionId
        };
        
        await nats.PublishAsync(NatsKeys.Disconnection, message, serializer: _serializer);
    
        logger.LogInformation("Пользователь отключен: {ConnectionId}", connectionId);
        
        await base.OnDisconnectedAsync(exception);
    }
    
    public async Task UpdateActivity(ActivityRequest activityRequest)
    {
        var connectionId = Context.ConnectionId;
        var now = DateTimeOffset.UtcNow;
        
        var message = new UserSessionMessage
        {
            ConnectionId = connectionId,
            Session = new UserSession
            {
                UserId = activityRequest.UserId,
                UpdatedAt = now
            }
        };
        
        await nats.PublishAsync(NatsKeys.UpdateActivity, message, serializer: _serializer);
    }

    // public async Task ClickChannel(ClickChannel dto)
    // {
    //     await statisticStore.SaveClickAsync(dto);
    // }
    
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
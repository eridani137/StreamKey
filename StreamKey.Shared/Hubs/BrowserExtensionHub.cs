using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using NATS.Client.Core;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.DTOs.Telegram;
using StreamKey.Shared.Entities;

namespace StreamKey.Shared.Hubs;

public class BrowserExtensionHub(
    INatsConnection nats,
    IMemoryCache cache,
    JsonNatsSerializer<UserSessionMessage> userSessionMessageSerializer,
    JsonNatsSerializer<ClickChannelRequest> clickChannelRequestSerializer,
    JsonNatsSerializer<ClickButtonRequest> clickButtonRequestSerializer,
    JsonNatsSerializer<TelegramUserRequest> telegramUserRequestSerializer,
    JsonNatsSerializer<TelegramUserDto?> telegramUserDtoSerializer,
    JsonNatsSerializer<List<ChannelDto>?> channelsResponseSerializer,
    JsonNatsSerializer<CheckMemberRequest> checkMemberRequestSerializer,
    JsonNatsSerializer<List<ButtonDto>?> buttonsResponseSerializer,
    JsonNatsSerializer<int> intSerializer
    // ILogger<BrowserExtensionHub> logger
)
    : Hub<IBrowserExtensionHub>
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> RegistrationTimeouts = new();

    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);

    private const string ChannelsCacheKey = "channels_list";

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var cts = new CancellationTokenSource();
        RegistrationTimeouts[connectionId] = cts;

        // logger.LogInformation("Новое подключение: {ConnectionId}", connectionId);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ConnectionTimeout, cts.Token);
                // logger.LogWarning("Таймаут регистрации пользователя: {ConnectionId}", connectionId);
                Context.Abort();
            }
            catch (TaskCanceledException)
            {
            }
        }, cts.Token);

        await Clients.Caller.RequestUserData();
        await base.OnConnectedAsync();
    }


    public async Task EntranceUserData(Guid sessionId)
    {
        var connectionId = Context.ConnectionId;
        var now = DateTimeOffset.UtcNow;

        var sessionMessage = new UserSessionMessage
        {
            ConnectionId = connectionId,
            Session = new UserSession
            {
                SessionId = sessionId,
                StartedAt = now
            }
        };

        await nats.PublishAsync(NatsKeys.Connection, sessionMessage, serializer: userSessionMessageSerializer);
        CancelRegistrationTimeout(connectionId);

        // logger.LogInformation("Пользователь зарегистрирован: {@UserData}", userData);
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

        await nats.PublishAsync(NatsKeys.Disconnection, message, serializer: userSessionMessageSerializer);

        // logger.LogInformation("Пользователь отключен: {ConnectionId}", connectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task UpdateActivity(UpdateUserActivityRequest updateUserActivityRequest)
    {
        var connectionId = Context.ConnectionId;

        var message = new UserSessionMessage
        {
            ConnectionId = connectionId,
            Session = new UserSession
            {
                UserId = updateUserActivityRequest.UserId
            }
        };

        await nats.PublishAsync(NatsKeys.UpdateActivity, message, serializer: userSessionMessageSerializer);
    }

    public async Task ClickChannel(ClickChannelRequest dto)
    {
        await nats.PublishAsync(NatsKeys.ClickChannel, dto, serializer: clickChannelRequestSerializer);
    }

    public async Task ClickButton(ClickButtonRequest dto)
    {
        await nats.PublishAsync(NatsKeys.ClickButton, dto, serializer: clickButtonRequestSerializer);
    }

    public async Task<TelegramUserDto?> GetTelegramUser(TelegramUserRequest request)
    {
        var response = await nats.RequestAsync(
            subject: NatsKeys.GetTelegramUser,
            data: request,
            headers: null,
            requestSerializer: telegramUserRequestSerializer,
            replySerializer: telegramUserDtoSerializer,
            requestOpts: new NatsPubOpts(),
            replyOpts: new NatsSubOpts { Timeout = TimeSpan.FromSeconds(15) }
        );

        return response.Data;
    }

    public async Task<List<ChannelDto>> GetChannels()
    {
        if (cache.TryGetValue<List<ChannelDto>>(ChannelsCacheKey, out var cachedChannels))
        {
            return cachedChannels!;
        }

        var response = await nats.RequestAsync<string?, List<ChannelDto>?>(
            subject: NatsKeys.GetChannels,
            data: "c",
            replySerializer: channelsResponseSerializer,
            requestOpts: new NatsPubOpts(),
            replyOpts: new NatsSubOpts { Timeout = TimeSpan.FromSeconds(15) }
        );

        var channels = response.Data ?? [];

        cache.Set(ChannelsCacheKey, channels, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3)
        });

        return channels;
    }

    public async Task CheckMember(CheckMemberRequest request)
    {
        var response = await nats.RequestAsync(
            subject: NatsKeys.CheckTelegramMember,
            data: request,
            requestSerializer: checkMemberRequestSerializer,
            replySerializer: telegramUserDtoSerializer,
            requestOpts: new NatsPubOpts(),
            replyOpts: new NatsSubOpts { Timeout = TimeSpan.FromSeconds(30) }
        );

        if (response.Data is not null)
        {
            await Clients.Caller.ReloadUserData(response.Data);
        }
    }

    // public async Task<List<ButtonDto>> GetButtons()
    // {
    //     return await GetButtonsByPosition(ButtonPosition.StreamBottom);
    // }

    public async Task<List<ButtonDto>> GetButtonsByPosition(ButtonPosition position)
    {
        var cacheKey = GetButtonsCacheKey(position);
        var subject = GetButtonsSubject(position);

        if (cache.TryGetValue(cacheKey, out List<ButtonDto>? cached))
        {
            return cached!;
        }

        var response = await nats.RequestAsync<object?, List<ButtonDto>?>(
            subject,
            null,
            replySerializer: buttonsResponseSerializer,
            replyOpts: new NatsSubOpts { Timeout = TimeSpan.FromSeconds(15) }
        );

        var buttons = response.Data ?? [];

        cache.Set(
            cacheKey,
            buttons,
            TimeSpan.FromMinutes(3));

        return buttons;
    }

    public static string GetButtonsCacheKey(ButtonPosition position)
    {
        return position switch
        {
            ButtonPosition.StreamBottom => "stream_bottom_buttons_list",
            ButtonPosition.LeftTopMenu => "left_top_buttons_list",
            ButtonPosition.TopChat => "top_chat_buttons_list",
            _ => throw new ArgumentOutOfRangeException(nameof(position))
        };
    }

    public static string GetButtonsSubject(ButtonPosition position)
    {
        return position switch
        {
            ButtonPosition.StreamBottom => NatsKeys.GetStreamBottomButtons,
            ButtonPosition.LeftTopMenu => NatsKeys.GetLeftTopButtons,
            ButtonPosition.TopChat => NatsKeys.GetTopChatButtons,
            _ => throw new ArgumentOutOfRangeException(nameof(position))
        };
    }
}
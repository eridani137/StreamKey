using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.BackgroundServices;

public class TelegramListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    MessagePackNatsSerializer<TelegramUserRequest> telegramUserRequestSerializer,
    MessagePackNatsSerializer<TelegramUserDto?> telegramUserDtoSerializer,
    ILogger<TelegramListener> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync(
            NatsKeys.GetTelegramUser,
            serializer: telegramUserRequestSerializer,
            cancellationToken: stoppingToken
        );

        await foreach (var msg in subscription)
        {
            try
            {
                var response = await FetchTelegramUserAsync(msg.Data!, stoppingToken);
                await nats.PublishAsync(msg.ReplyTo!, response, serializer: telegramUserDtoSerializer,
                    cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "Ошибка при обработке NATS-сообщения: {Subject}",
                    msg.Subject);
                
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await nats.PublishAsync(
                            msg.ReplyTo, 
                            null, 
                            serializer: telegramUserDtoSerializer,
                            cancellationToken: stoppingToken
                        );
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Не удалось отправить error response");
                    }
                }
            }
        }
    }

    private async Task<TelegramUserDto?> FetchTelegramUserAsync(TelegramUserRequest request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();

        var user = await repository.GetByTelegramIdNotTracked(request.UserId, cancellationToken);
        if (user is null) return null;

        if (!string.Equals(request.UserHash, user.Hash, StringComparison.Ordinal))
        {
            return null;
        }

        var userDto = user.MapUserDto();
        return userDto;
    }
}
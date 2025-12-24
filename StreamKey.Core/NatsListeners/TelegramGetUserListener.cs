using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.NatsListeners;

public class TelegramGetUserListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    INatsRequestReplyProcessor<TelegramUserRequest, TelegramUserDto?> processor,
    JsonNatsSerializer<TelegramUserRequest> telegramUserRequestSerializer,
    JsonNatsSerializer<TelegramUserDto?> telegramUserDtoSerializer
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync(
            NatsKeys.GetTelegramUser,
            serializer: telegramUserRequestSerializer,
            cancellationToken: stoppingToken
        );

        await processor.ProcessAsync(
            subscription,
            request => FetchTelegramUserAsync(request, stoppingToken),
            nats,
            telegramUserDtoSerializer,
            stoppingToken
        );
    }

    private async Task<TelegramUserDto?> FetchTelegramUserAsync(TelegramUserRequest request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();

        var user = await repository.GetByTelegramIdNotTracked(request.UserId, cancellationToken);

        return user?.MapUserDto();
    }
}
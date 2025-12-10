using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Extensions;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared;
using StreamKey.Shared.DTOs.Telegram;

namespace StreamKey.Core.NatsListeners;

public class CheckTelegramMemberListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    MessagePackNatsSerializer<CheckMemberRequest> requestSerializer,
    MessagePackNatsSerializer<TelegramUserDto?> responseSerializer,
    INatsRequestReplyProcessor<CheckMemberRequest, TelegramUserDto?> processor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync(
            NatsKeys.CheckTelegramMember,
            serializer: requestSerializer,
            cancellationToken: stoppingToken
        );

        await processor.ProcessAsync(
            subscription,
            request => CheckMemberAsync(request, stoppingToken),
            nats,
            responseSerializer,
            stoppingToken
        );
    }

    private async Task<TelegramUserDto?> CheckMemberAsync(
        CheckMemberRequest request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ITelegramUserRepository>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await repository.GetByTelegramId(request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var getChatMemberResponse = await telegramService.GetChatMember(request.UserId, cancellationToken);
        if (getChatMemberResponse is null)
        {
            return user.MapUserDto();
        }

        var isChatMember = getChatMemberResponse.IsChatMember();
        if (user.IsChatMember != isChatMember)
        {
            user.IsChatMember = isChatMember;
            user.UpdatedAt = DateTime.UtcNow;

            repository.Update(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return user.MapUserDto();
    }
}
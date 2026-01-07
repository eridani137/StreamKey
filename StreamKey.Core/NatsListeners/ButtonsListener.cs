using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;

namespace StreamKey.Core.NatsListeners;

public class ButtonsListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    JsonNatsSerializer<List<ButtonDto>?> responseSerializer,
    INatsRequestReplyProcessor<string?, List<ButtonDto>?> processor
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync<string?>(
            NatsKeys.GetButtons,
            cancellationToken: stoppingToken);

        await processor.ProcessAsync(
            subscription,
            _ => GetButtonsAsync(stoppingToken),
            nats,
            responseSerializer,
            stoppingToken);
    }

    private async Task<List<ButtonDto>?> GetButtonsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IButtonService>();

        var entities = await service.GetButtons(cancellationToken);
        return entities
            .Where(b => b.IsEnabled)
            .Select(b => b.Map())
            .ToList();
    }
}
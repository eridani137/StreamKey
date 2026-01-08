using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;

namespace StreamKey.Core.NatsListeners;

public class ButtonsListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    JsonNatsSerializer<List<ButtonDto>?> responseSerializer,
    INatsRequestReplyProcessor<int, List<ButtonDto>?> processor
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var subscription = nats.SubscribeAsync<int>(
            NatsKeys.GetButtons,
            cancellationToken: stoppingToken);

        await processor.ProcessAsync(
            subscription,
            position => GetButtonsAsync(position, stoppingToken),
            nats,
            responseSerializer,
            stoppingToken);
    }

    private async Task<List<ButtonDto>?> GetButtonsAsync(int position, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IButtonService>();

        var entities = await service.GetButtons((ButtonPosition)position, cancellationToken);
        return entities
            .Select(b => b.Map())
            .ToList();
    }
}
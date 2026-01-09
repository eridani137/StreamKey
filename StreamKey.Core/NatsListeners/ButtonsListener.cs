using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Mappers;
using StreamKey.Shared;
using StreamKey.Shared.DTOs;
using StreamKey.Shared.Entities;
using StreamKey.Shared.Hubs;

namespace StreamKey.Core.NatsListeners;

public class ButtonsListener(
    IServiceScopeFactory scopeFactory,
    INatsConnection nats,
    JsonNatsSerializer<List<ButtonDto>?> responseSerializer,
    INatsRequestReplyProcessor<object?, List<ButtonDto>?> processor
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var positions = Enum.GetValues<ButtonPosition>();

        var tasks = positions.Select(position =>
        {
            var pos = position;

            var subject = BrowserExtensionHub.GetButtonsSubject(pos);
            var subscription = nats.SubscribeAsync<object?>(
                subject,
                cancellationToken: stoppingToken
            );

            return processor.ProcessAsync(
                subscription,
                _ => GetButtonsAsync(pos, stoppingToken),
                nats,
                responseSerializer,
                stoppingToken
            );
        });

        await Task.WhenAll(tasks);
    }

    private async Task<List<ButtonDto>?> GetButtonsAsync(
        ButtonPosition position,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IButtonService>();

        var entities = await service.GetButtons(position, cancellationToken);

        return entities
            .Where(b => b.IsEnabled)
            .Select(b => b.Map())
            .ToList();
    }
}
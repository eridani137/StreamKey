using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    INatsRequestReplyProcessor<object?, List<ButtonDto>?> processor,
    ILogger<ButtonsListener> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var positions = Enum.GetValues<ButtonPosition>();

        var tasks = positions.Select(position =>
        {
            var currentPosition = position;
            var subject = BrowserExtensionHub.GetButtonsSubject(currentPosition);
            
            logger.LogInformation("Starting listener for position {Position} on subject {Subject}", 
                currentPosition, subject);

            var subscription = nats.SubscribeAsync<object?>(
                subject,
                cancellationToken: stoppingToken
            );

            return processor.ProcessAsync(
                subscription,
                _ =>
                {
                    logger.LogInformation("Received request for position {Position}", currentPosition);
                    return GetButtonsAsync(currentPosition, stoppingToken);
                },
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
        logger.LogInformation("GetButtonsAsync called for position: {Position}", position);
        
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IButtonService>();

        var entities = await service.GetButtons(position, cancellationToken);
        
        logger.LogInformation("Found {Count} buttons for position {Position}", 
            entities.Count, position);

        var result = entities
            .Where(b => b.IsEnabled)
            .Select(b =>
            {
                var dto = b.Map();
                logger.LogDebug("Mapped button {Id} with position {Position}", 
                    dto.Id, dto.Position);
                return dto;
            })
            .ToList();
            
        logger.LogInformation("Returning {Count} enabled buttons for position {Position}", 
            result.Count, position);

        return result;
    }
}
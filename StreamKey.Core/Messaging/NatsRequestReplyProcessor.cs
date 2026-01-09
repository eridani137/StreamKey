using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;

namespace StreamKey.Core.Messaging;

public sealed class NatsRequestReplyProcessor<TRequest, TResponse>(
    ILogger<NatsRequestReplyProcessor<TRequest, TResponse>> logger)
    : INatsRequestReplyProcessor<TRequest, TResponse>
{
    public async Task ProcessAsync(
        IAsyncEnumerable<NatsMsg<TRequest?>> subscription,
        Func<TRequest?, Task<TResponse>> handle,
        INatsConnection nats,
        INatsSerialize<TResponse?>? responseSerializer,
        CancellationToken token)
    {
        await foreach (var msg in subscription.WithCancellation(token))
        {
            try
            {
                // logger.LogDebug("Обработка запроса: {Subject}", msg.Subject);

                var response = await handle(msg.Data);
                await SendResponseAsync(nats, msg.ReplyTo, response, responseSerializer, token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при обработке NATS-сообщения: {Subject}", msg.Subject);
                
                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await nats.PublishAsync(
                            msg.ReplyTo,
                            default,
                            serializer: responseSerializer,
                            cancellationToken: token);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Не удалось отправить error response на {ReplyTo}", msg.ReplyTo);
                    }
                }
            }
        }
    }

    private async Task SendResponseAsync(
        INatsConnection nats,
        string? replyTo,
        TResponse? response,
        INatsSerialize<TResponse?>? serializer,
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(replyTo))
        {
            logger.LogWarning("ReplyTo отсутствует, ответ не будет отправлен");
            return;
        }

        await nats.PublishAsync(replyTo, response, serializer: serializer, cancellationToken: token);
    }
}
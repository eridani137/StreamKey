using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using StreamKey.Core.Abstractions;

namespace StreamKey.Core.Messaging;

public sealed class NatsRequestReplyProcessor<TRequest, TResponse>(
    ILogger<NatsRequestReplyProcessor<TRequest, TResponse>> logger)
    : INatsRequestReplyProcessor<TRequest, TResponse>
{
    public async Task ProcessAsync(
        IAsyncEnumerable<NatsMsg<TRequest>> subscription,
        Func<TRequest, Task<TResponse>> handle,
        INatsConnection nats,
        CancellationToken token)
    {
        await foreach (var msg in subscription.WithCancellation(token))
        {
            try
            {
                if (msg.Data is null)
                {
                    logger.LogWarning("Получен запрос с null данными: {Subject}", msg.Subject);
                    await SendResponseAsync(nats, msg.ReplyTo, default, token);
                    continue;
                }

                var response = await handle(msg.Data);
                await SendResponseAsync(nats, msg.ReplyTo, response, token);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при обработке NATS-сообщения: {Subject}", msg.Subject);

                if (!string.IsNullOrEmpty(msg.ReplyTo))
                {
                    try
                    {
                        await nats.PublishAsync<TResponse?>(
                            msg.ReplyTo,
                            default,
                            cancellationToken: token
                        );
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
        CancellationToken token)
    {
        if (string.IsNullOrEmpty(replyTo))
        {
            logger.LogWarning("ReplyTo отсутствует, ответ не будет отправлен");
            return;
        }

        await nats.PublishAsync(replyTo, response, cancellationToken: token);
    }
}
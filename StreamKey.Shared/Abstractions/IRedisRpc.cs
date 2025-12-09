namespace StreamKey.Shared.Abstractions;

public interface IRedisRpc
{
    Task<TResponse?> CallAsync<TRequest, TResponse>(
        string method,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
namespace StreamKey.Shared.Abstractions;

public interface IRedisRpcHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken token);
}
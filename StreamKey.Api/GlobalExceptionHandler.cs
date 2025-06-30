using Microsoft.AspNetCore.Diagnostics;

namespace StreamKey.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Необработанное исключение");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return ValueTask.FromResult(true);
    }
}
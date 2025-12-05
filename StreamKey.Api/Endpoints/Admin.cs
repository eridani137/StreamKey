using Carter;
using StreamKey.Core.Hubs;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Admin : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin")
            .WithTags("Администрирование")
            .RequireAuthorization();

        group.MapGet("/restart",
                async (IHostApplicationLifetime appLifetime,
                    ILogger<Admin> logger,
                    IRestartRepository repository,
                    IUnitOfWork unitOfWork,
                    CancellationToken cancellationToken) =>
                {
                    logger.LogInformation("Перезапуск по запросу");

                    await repository.Add(new RestartEntity
                    {
                        DateTime = DateTime.UtcNow
                    }, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    appLifetime.StopApplication();
                })
            .WithSummary("Перезапуск");

        group.MapGet("/users", () => Results.Json(BrowserExtensionHub.Users.ToList()))
            .WithSummary("Получение подключенных пользователей");
    }
}
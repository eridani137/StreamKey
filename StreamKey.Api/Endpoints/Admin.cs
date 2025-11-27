using Carter;
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
                    IUnitOfWork unitOfWork) =>
                {
                    logger.LogInformation("Перезапуск по запросу");

                    await repository.Add(new RestartEntity
                    {
                        DateTime = DateTime.UtcNow
                    });
                    await unitOfWork.SaveChangesAsync();

                    appLifetime.StopApplication();
                })
            .WithSummary("Перезапуск");
    }
}
using Carter;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Api.Endpoints;

public class Settings : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings")
            .WithTags("Управление настройками")
            .RequireAuthorization();

        group.MapGet("", (ISettingsStorage settings) => Task.FromResult(Results.Ok(settings.GetAllKeysAsync())))
            .WithName("Получить ключи");
    }
}
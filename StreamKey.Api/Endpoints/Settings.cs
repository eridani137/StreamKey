using Carter;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Settings : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settings")
            .WithTags("Работа с настройками")
            .RequireAuthorization();

        group.MapGet("",
                async (ISettingsRepository repository) => Results.Ok(await repository.GetAll()))
            .Produces(StatusCodes.Status200OK);

        group.MapPost("",
                async (SettingsEntity entity, ISettingsRepository repository) =>
                {
                    await repository.SetValue(entity.Key, entity.Value);
                })
            .Produces(StatusCodes.Status200OK);
        ;

        group.MapDelete("/{key}",
                async (string key, ISettingsRepository repository) => { await repository.Remove(key); })
            .Produces(StatusCodes.Status200OK);
        ;
    }
}
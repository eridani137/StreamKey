using Carter;
using Microsoft.AspNetCore.SignalR;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Extensions;
using StreamKey.Core.Hubs;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Api.Endpoints;

public class Telegram : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/telegram")
            .WithTags("Взаимодействие с Telegram");

        group.MapPost("/login",
                async (TelegramAuthDto dto, Guid sessionId, ITelegramService service,
                    ITelegramUserRepository repository, IUnitOfWork unitOfWork,
                    IHubContext<BrowserExtensionHub, IBrowserExtensionHub> extensionHub) =>
                {
                    var user = await repository.GetByTelegramId(dto.Id);

                    var isNewUser = false;

                    if (user is null)
                    {
                        user = dto.Map();
                        isNewUser = true;
                    }

                    var getChatMemberResponse = await service.GetChatMember(dto.Id);
                    if (getChatMemberResponse is null) return Results.BadRequest("response is null");

                    user.FirstName = dto.FirstName;

                    user.Username = dto.Username;

                    user.AuthDate = dto.AuthDate;

                    user.PhotoUrl = dto.PhotoUrl;

                    user.Hash = dto.Hash;

                    user.IsChatMember = getChatMemberResponse.IsChatMember();

                    user.AuthorizedAt = DateTime.UtcNow;

                    if (isNewUser)
                    {
                        await repository.Add(user);
                    }
                    else
                    {
                        repository.Update(user);
                    }

                    await unitOfWork.SaveChangesAsync();

                    var client = BrowserExtensionHub.Users.FirstOrDefault(kvp => kvp.Value.SessionId == sessionId);
                    if (client.Key is not null)
                    {
                        await extensionHub.Clients.Client(client.Key).ReloadUserData(dto.MapUserDto(user.IsChatMember));
                    }

                    return Results.Ok();
                })
            .WithSummary("Авторизация");

        var userGroup = group.MapGroup("/user");

        userGroup.MapGet("/{id:long}/{hash}",
                async (long id, string hash, ITelegramUserRepository repository) =>
                {
                    var user = await repository.GetByTelegramIdNotTracked(id);
                    if (user is null) return Results.NotFound();

                    if (!string.Equals(hash, user.Hash, StringComparison.Ordinal))
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(user.MapUserDto());
                })
            .Produces<TelegramAuthDto>()
            .WithSummary("Получение данных о пользователе");

        userGroup.MapGet("/{id:long}",
                async (long id, ITelegramService service) =>
                {
                    var getChatMemberResponse = await service.GetChatMember(id);
                    return getChatMemberResponse is null
                        ? Results.BadRequest(getChatMemberResponse)
                        : Results.Ok(getChatMemberResponse);
                })
            .Produces<GetChatMemberResponse?>()
            .WithSummary("Проверка подписки на канал");
    }
}
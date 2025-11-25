using Carter;
using StreamKey.Core.DTOs;
using StreamKey.Core.Mappers;
using StreamKey.Core.Services;
using StreamKey.Infrastructure.Abstractions;

namespace StreamKey.Api.Endpoints;

public class Telegram : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/telegram")
            .WithTags("Взаимодействие с Telegram");

        group.MapPost("/login",
                async (TelegramAuthDto dto, ITelegramService service, ITelegramUserRepository repository,
                    IUnitOfWork unitOfWork) =>
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

                    user.IsChatMember = getChatMemberResponse?.Result?.Status is ChatMemberStatus.Creator or ChatMemberStatus.Owner or ChatMemberStatus.Administrator or ChatMemberStatus.Member or ChatMemberStatus.Restricted;

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

                    return user.IsChatMember
                        ? Results.Ok()
                        : Results.BadRequest("user is not member");
                })
            .WithSummary("Авторизация");
    }
}
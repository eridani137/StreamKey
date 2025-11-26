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
                async (TelegramAuthDto dto, ITelegramService service, ITelegramUserRepository repository, IUnitOfWork unitOfWork) =>
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
                    
                    user.IsChatMember = getChatMemberResponse.Result?.Status is ChatMemberStatus.Creator
                        or ChatMemberStatus.Owner or ChatMemberStatus.Administrator or ChatMemberStatus.Member
                        or ChatMemberStatus.Restricted;

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

                    return Results.Ok();
                })
            .WithSummary("Авторизация");

        group.MapGet("/user/{id:long}/{hash}",
                async (long id, string hash, ITelegramUserRepository repository) =>
                {
                    var user = await repository.GetByTelegramId(id);
                    if (user is null) return Results.NotFound();

                    if (!string.Equals(hash, user.Hash, StringComparison.Ordinal))
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(user.MapUserDto());
                })
            .Produces<TelegramAuthDto>()
            .WithSummary("Получение данных о пользователе");
    }
}
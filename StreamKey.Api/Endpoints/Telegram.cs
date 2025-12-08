using Carter;
using Microsoft.AspNetCore.SignalR;
using StreamKey.Core.Abstractions;
using StreamKey.Core.BackgroundServices;
using StreamKey.Core.Mappers;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Shared.DTOs;

namespace StreamKey.Api.Endpoints;

public class Telegram : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/telegram")
            .WithTags("Взаимодействие с Telegram");

        group.MapPost("/login/{sessionId:guid}",
                (TelegramAuthDto dto, Guid sessionId) =>
                {
                    if (string.IsNullOrEmpty(dto.Hash))
                    {
                        return Results.BadRequest();
                    }

                    if (TelegramHandler.NewUsers.Any(u => u.Id == dto.Id))
                    {
                        return Results.BadRequest("Запрос уже в обработке");
                    }

                    TelegramHandler.NewUsers.Enqueue(new TelegramAuthDtoWithSessionId(dto, sessionId));

                    return Results.Ok();
                })
            .WithSummary("Авторизация");

        var userGroup = group.MapGroup("/user");

        userGroup.MapGet("/{id:long}/{hash}",
                async (long id, string hash, ITelegramUserRepository repository, CancellationToken cancellationToken) =>
                {
                    var user = await repository.GetByTelegramIdNotTracked(id, cancellationToken);
                    if (user is null) return Results.NotFound();

                    if (!string.Equals(hash, user.Hash, StringComparison.Ordinal))
                    {
                        return Results.Forbid();
                    }

                    return Results.Ok(user.MapUserDto());
                })
            .WithSummary("Получение данных о пользователе");

        userGroup.MapGet("/{id:long}",
                async (long id, ITelegramService service, CancellationToken cancellationToken) =>
                {
                    var getChatMemberResponse = await service.GetChatMember(id, cancellationToken);
                    if (getChatMemberResponse is null) return Results.BadRequest("Chat member check failed");

                    return Results.Ok(getChatMemberResponse);
                })
            .Produces<GetChatMemberResponse?>()
            .WithSummary("Проверка подписки на канал");
    }

    // private static string CheckHash(TelegramAuthDto dto)
    // {
    //     var dataCheckList = new List<string>();
    //
    //     if (dto.Id > 0) dataCheckList.Add($"id={dto.Id}");
    //
    //     if (dto.AuthDate > 0) dataCheckList.Add($"auth_date={dto.AuthDate}");
    //
    //     if (!string.IsNullOrEmpty(dto.FirstName)) dataCheckList.Add($"first_name={dto.FirstName}");
    //
    //     if (!string.IsNullOrEmpty(dto.Username)) dataCheckList.Add($"username={dto.Username}");
    //
    //     if (!string.IsNullOrEmpty(dto.PhotoUrl)) dataCheckList.Add($"photo_url={dto.PhotoUrl}");
    //
    //     dataCheckList.Sort();
    //
    //     var dataCheckString = string.Join("\n", dataCheckList);
    //
    //     var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(ApplicationConstants.TelegramBotToken));
    //
    //     byte[] hashBytes;
    //     using (var hmac = new HMACSHA256(secretKey))
    //     {
    //         hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
    //     }
    //
    //     var hash = Convert.ToHexStringLower(hashBytes);
    //
    //     if (!hash.Equals(dto.Hash, StringComparison.Ordinal))
    //     {
    //         return "Хеш недействителен";
    //     }
    //
    //     var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    //     const long seconds = 86400;
    //
    //     if (currentTime - dto.AuthDate > seconds)
    //     {
    //         return "Срок действия истек";
    //     }
    //
    //     return string.Empty;
    // }
}
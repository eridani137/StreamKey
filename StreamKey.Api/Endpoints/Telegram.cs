using System.Data;
using Carter;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
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

        group.MapPost("/login/{sessionId:guid}",
                async (TelegramAuthDto dto, Guid sessionId, ITelegramService service,
                    ITelegramUserRepository repository, IUnitOfWork unitOfWork,
                    IHubContext<BrowserExtensionHub, IBrowserExtensionHub> extensionHub,
                    CancellationToken cancellationToken) =>
                {
                    await using var transaction =
                        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

                    try
                    {
                        var user = await repository.GetByTelegramId(dto.Id, cancellationToken);

                        if (user is null)
                        {
                            user = dto.Map();
                            await repository.Add(user, cancellationToken);
                        }

                        var chatMember = await service.GetChatMember(dto.Id, cancellationToken);
                        if (chatMember is null) return Results.BadRequest("Chat member check failed");

                        user.FirstName = dto.FirstName;
                        user.Username = dto.Username;
                        user.AuthDate = dto.AuthDate;
                        user.PhotoUrl = dto.PhotoUrl;
                        user.Hash = dto.Hash;
                        user.IsChatMember = chatMember.IsChatMember();
                        user.AuthorizedAt = DateTime.UtcNow;

                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);

                        if (BrowserExtensionHub.GetConnectionIdBySessionId(sessionId) is { } connectionId)
                        {
                            await extensionHub.Clients.Client(connectionId)
                                .ReloadUserData(dto.MapUserDto(user.IsChatMember));
                        }

                        return Results.Ok();
                    }
                    catch (PostgresException ex) when (ex.SqlState == "23505")
                    {
                        return Results.Conflict("User already exists");
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Results.BadRequest();
                    }
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
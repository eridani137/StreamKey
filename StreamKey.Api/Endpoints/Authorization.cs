using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Carter;
using Microsoft.AspNetCore.Identity;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
using StreamKey.Core.Services;
using StreamKey.Shared;
using StreamKey.Shared.Entities;

namespace StreamKey.Api.Endpoints;

public class Authorization : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Аутентификация");

        group.MapPost("/login",
                async (
                    LoginRequest login,
                    UserManager<ApplicationUser> userManager,
                    IJwtService jwtService) =>
                {
                    var user = await userManager.FindByNameAsync(login.Username);
                    if (user is null) return Results.NotFound();

                    var result = await userManager.CheckPasswordAsync(user, login.Password);

                    // if (result.RequiresTwoFactor)
                    // {
                    //     if (!string.IsNullOrEmpty(login.TwoFactorCode))
                    //     {
                    //         result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, false,
                    //             false);
                    //     }
                    //     else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
                    //     {
                    //         result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
                    //     }
                    // }

                    if (!result)
                    {
                        return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized);
                    }

                    await userManager.ResetAccessFailedCountAsync(user);

                    var token = jwtService.GenerateToken(user);

                    return Results.Ok(token);
                })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>()
            .WithSummary("Авторизация");

        group.MapPost("/telegram/login/check",
                async (TelegramAuthDto dto, ITelegramService service, HttpResponse response) =>
                {
                    var dataCheckList = new List<string>();

                    if (dto.Id > 0) dataCheckList.Add($"id={dto.Id}");

                    if (dto.AuthDate > 0) dataCheckList.Add($"auth_date={dto.AuthDate}");

                    if (!string.IsNullOrEmpty(dto.FirstName)) dataCheckList.Add($"first_name={dto.FirstName}");

                    if (!string.IsNullOrEmpty(dto.Username)) dataCheckList.Add($"username={dto.Username}");

                    if (!string.IsNullOrEmpty(dto.PhotoUrl)) dataCheckList.Add($"photo_url={dto.PhotoUrl}");

                    dataCheckList.Sort();

                    var dataCheckString = string.Join("\n", dataCheckList);

                    var secretKey = SHA256.HashData(
                        Encoding.UTF8.GetBytes(ApplicationConstants.TelegramBotToken)
                    );

                    byte[] hashBytes;
                    using (var hmac = new HMACSHA256(secretKey))
                    {
                        hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
                    }

                    var hash = Convert.ToHexStringLower(hashBytes);

                    if (!hash.Equals(dto.Hash, StringComparison.Ordinal))
                    {
                        return Results.BadRequest("hash does not match");
                    }

                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    // const long seconds = 86400;
                    const long seconds = 31_536_000;
                    
                    if (currentTime - dto.AuthDate > seconds)
                    {
                        return Results.BadRequest("data is outdated");
                    }

                    var getChatMemberResponse = await service.GetChatMember(dto.Id);
                    if (getChatMemberResponse is null) return Results.BadRequest("response is null");

                    if (getChatMemberResponse?.Result?.Status is not (ChatMemberStatus.Creator or ChatMemberStatus.Owner
                        or ChatMemberStatus.Administrator or ChatMemberStatus.Member or ChatMemberStatus.Restricted))
                    {
                        return Results.NotFound("user is not member");
                    }

                    var cookieValue = JsonSerializer.Serialize(dto);
                    
                    response.Cookies.Append("tg-auth", cookieValue, new CookieOptions()
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = DateTimeOffset.UtcNow.AddYears(1)
                    });
                    
                    return Results.Ok();
                })
            .WithSummary("Проверка пользователя Telegram");
    }
}
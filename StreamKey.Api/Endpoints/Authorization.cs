using System.Security.Cryptography;
using System.Text;
using Carter;
using Microsoft.AspNetCore.Identity;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
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

        group.MapPost("/telegram/login",
            (TelegramAuthDto dto) =>
            {
                var checkHash = dto.Hash;

                var dataCheckList = new List<string>
                {
                    $"username={dto.Username}",
                    $"first_name={dto.FirstName}",
                    $"last_name={dto.LastName}",
                    $"photo_url={dto.PhotoUrl}",
                    $"auth_date={dto.AuthDate}",
                    $"id={dto.Id}"
                };

                dataCheckList.Sort();

                var dataCheckString = string.Join("\n", dataCheckList);

                var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(ApplicationConstants.TelegramAuthorizationBotToken));

                byte[] hashBytes;
                using (var hmac = new HMACSHA256(secretKey))
                {
                    hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
                }

                var hash = Convert.ToHexStringLower(hashBytes);

                if (hash.Equals(checkHash, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest("hash does not match");
                }

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (currentTime - dto.AuthDate > 86400)
                {
                    return Results.BadRequest("data is outdated");
                }

                return Results.Ok(dto);
            });
    }
}
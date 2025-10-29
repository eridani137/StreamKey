using Carter;
using Microsoft.AspNetCore.Identity;
using StreamKey.Core.Abstractions;
using StreamKey.Core.DTOs;
using StreamKey.Core.Filters;
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
            .WithDescription("Авторизация");
    }
}
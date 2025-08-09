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
                    SignInManager<ApplicationUser> signInManager,
                    UserManager<ApplicationUser> userManager,
                    IJwtService jwtService) =>
                {
                    var user = await userManager.FindByNameAsync(login.Username);
                    if (user is null) return Results.NotFound();

                    var result = await signInManager.PasswordSignInAsync(login.Username, login.Password, false, true);

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

                    if (!result.Succeeded)
                    {
                        return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
                    }
                    
                    await userManager.ResetAccessFailedCountAsync(user);
                    
                    var token = jwtService.GenerateToken(user, await userManager.GetRolesAsync(user));

                    return Results.Ok(token);
                })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }
}
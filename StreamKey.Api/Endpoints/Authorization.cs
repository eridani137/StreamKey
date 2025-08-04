using Carter;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
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
            async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>>
                (LoginRequest login, SignInManager<ApplicationUser> signInManager) =>
            {
                signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;

                var result = await signInManager.PasswordSignInAsync(login.Email, login.Password, false, true);

                if (result.RequiresTwoFactor)
                {
                    if (!string.IsNullOrEmpty(login.TwoFactorCode))
                    {
                        result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, false,
                            false);
                    }
                    else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
                    {
                        result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
                    }
                }

                if (!result.Succeeded)
                {
                    return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
                }

                return TypedResults.Empty;
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();
    }
}
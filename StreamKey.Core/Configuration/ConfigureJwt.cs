using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StreamKey.Shared.Configs;

namespace StreamKey.Core.Configuration;

public static class ConfigureJwt
{
    public static void Configure(WebApplicationBuilder builder)
    {
        var jwtConfig = builder.Configuration.GetSection(nameof(JwtConfig)).Get<JwtConfig>();
        if (jwtConfig is null)
        {
            throw new ApplicationException("JWT конфигурация не заполнена");
        }
        
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection(nameof(JwtConfig)));
        
        var secret = Encoding.UTF8.GetBytes(jwtConfig.Secret);

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secret),
                    ValidateIssuerSigningKey = true
                };
            });

        builder.Services.AddAuthorization();
    }
}
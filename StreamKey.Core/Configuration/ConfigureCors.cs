using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace StreamKey.Core.Configuration;

public static class ConfigureCors
{
    public const string ProductionCorsPolicyName = "CorsPolicy";

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(ProductionCorsPolicyName, policy =>
            {
                policy
                    .WithOrigins("https://streamkey.ru")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace StreamKey.Application.Configuration;

public static class CorsConfiguration
{
    public const string ProductionCorsPolicyName = "ProductionPolicy";
    
    public static void ConfigureCors(WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(ProductionCorsPolicyName, policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .WithHeaders("Content-Type", "User-Agent");
            });
        });
    }
}
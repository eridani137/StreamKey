using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace StreamKey.Core.Configuration;

public static class ConfigureCors
{
    public const string ProductionCorsPolicyName = "ProductionPolicy";
    
    public static void Configure(WebApplicationBuilder builder)
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
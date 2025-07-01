using Microsoft.Extensions.DependencyInjection;
using StreamKey.Application.Services;

namespace StreamKey.Application;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITwitchService, TwitchService>();
        services.AddScoped<IUsherService, UsherService>();
        
        return services;
    }
} 
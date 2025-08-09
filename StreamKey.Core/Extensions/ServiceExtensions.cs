using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Services;
using StreamKey.Core.Validation;

namespace StreamKey.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsherService, UsherService>();
        services.AddScoped<IChannelService, ChannelService>();
        
        services.AddValidatorsFromAssembly(typeof(IValidatorMarker).Assembly);
        
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddSingleton<ICamoufoxService, CamoufoxService>();

        services.AddScoped<IJwtService, JwtService>();
        
        return services;
    }
} 
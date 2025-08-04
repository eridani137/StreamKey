using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Services;
using StreamKey.Application.Validation;

namespace StreamKey.Application.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsherService, UsherService>();
        services.AddScoped<IChannelService, ChannelService>();
        
        services.AddValidatorsFromAssembly(typeof(IValidatorMarker).Assembly);
        
        return services;
    }
} 
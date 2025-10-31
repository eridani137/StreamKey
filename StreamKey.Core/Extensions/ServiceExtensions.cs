using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamKey.Core.Abstractions;
using StreamKey.Core.Services;
using StreamKey.Core.Validation;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Services;
using StreamKey.Shared;

namespace StreamKey.Core.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUsherService, UsherService>();
        services.AddScoped<ITwitchService, TwitchService>();
        
        services.AddScoped<IChannelService, ChannelService>();
        
        services.AddValidatorsFromAssembly(typeof(IValidatorMarker).Assembly);
        
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        services.AddSingleton<ICamoufoxService, CamoufoxService>();

        services.AddScoped<IJwtService, JwtService>();

        services.AddSingleton<StatisticService>();

        services.AddHostedService<ChannelHandler>();
        services.AddHostedService<StatisticHandler>();
        
        return services;
    }

    public static void AddAdditionHeaders(this IHostApplicationBuilder builder)
    {
        var authorization = builder.Configuration.GetSection("Authorization");
        if (authorization.Exists() && !string.IsNullOrEmpty(authorization.Value))
        {
            ApplicationConstants.Headers.Add("Authorization", authorization.Value);
        }
        
        var deviceId = builder.Configuration.GetSection("DeviceId");
        if (deviceId.Exists() && !string.IsNullOrEmpty(deviceId.Value))
        {
            ApplicationConstants.Headers.Add("x-device-id", deviceId.Value);
        }
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(ApplicationConstants.UsherClientName, (_, client) =>
            {
                client.BaseAddress = ApplicationConstants.UsherUrl;
                client.DefaultRequestHeaders.Referrer = new Uri(ApplicationConstants.TwitchUrl);

                foreach (var header in ApplicationConstants.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<FilterNotFoundHandler>()
            .AddStandardResilienceHandler();
        
        services.AddHttpClient(ApplicationConstants.ServerClientName, (_, client) =>
            {
                client.BaseAddress = ApplicationConstants.QqlUrl;
                client.DefaultRequestHeaders.Referrer = new Uri(ApplicationConstants.TwitchUrl);

                foreach (var header in ApplicationConstants.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<FilterNotFoundHandler>()
            .AddStandardResilienceHandler();
        
        services.AddHttpClient<ICamoufoxService, CamoufoxService>((_, client) =>
        {
            client.BaseAddress = new Uri("http://camoufox:8080");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddStandardResilienceHandler();

        return services;
    }
} 
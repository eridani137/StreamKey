using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamKey.Core.Abstractions;
using StreamKey.Core.BackgroundServices;
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
        services.AddSingleton<ITelegramService, TelegramService>();

        services.AddScoped<IJwtService, JwtService>();

        services.AddSingleton<StatisticService>();

        services.AddHostedService<ChannelHandler>();
        services.AddHostedService<StatisticHandler>();
        services.AddHostedService<RestartService>();

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
                client.DefaultRequestHeaders.Referrer = ApplicationConstants.TwitchUrl;

                foreach (var header in ApplicationConstants.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<FilterNotFoundHandler>();

        services.AddHttpClient(ApplicationConstants.ServerClientName, (_, client) =>
            {
                client.BaseAddress = ApplicationConstants.QqlUrl;
                client.DefaultRequestHeaders.Referrer = ApplicationConstants.TwitchUrl;

                foreach (var header in ApplicationConstants.Headers)
                {
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<FilterNotFoundHandler>();

        services.AddHttpClient<ICamoufoxService, CamoufoxService>((_, client) =>
        {
            client.BaseAddress = new Uri("http://camoufox:8080");
        });

        services.AddHttpClient(ApplicationConstants.TelegramClientName, (_, client) =>
        {
            client.BaseAddress = ApplicationConstants.TelegramUrl;
            client.DefaultRequestHeaders.Referrer = ApplicationConstants.TelegramUrl;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        return services;
    }
}
using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using StreamKey.Core.Abstractions;
using StreamKey.Core.BackgroundServices;
using StreamKey.Core.Services;
using StreamKey.Core.Validation;
using StreamKey.Infrastructure.Abstractions;
using StreamKey.Infrastructure.Services;
using StreamKey.Shared;
using StreamKey.Shared.Configs;

namespace StreamKey.Core.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
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
            services.AddHostedService<RestartHandler>();
            services.AddHostedService<TelegramHandler>();

            return services;
        }

        public IServiceCollection AddHttpClients()
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

            services.AddHttpClient(ApplicationConstants.TwitchClientName, (_, client) =>
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

    extension(IHostApplicationBuilder builder)
    {
        public void AddRedisBackplane(bool isInternal)
        {
            if (builder.Configuration.GetSection(nameof(RedisConfig)).Get<RedisConfig>() is { } redisConfig &&
                builder.Configuration.GetSection("RedisHost").Get<string>() is { } redisHost)
            {
                if (isInternal) redisHost = "localhost";
                builder.Services.AddSignalR()
                    .AddMessagePackProtocol()
                    .AddStackExchangeRedis(options =>
                    {
                        options.Configuration = new ConfigurationOptions
                        {
                            EndPoints = { $"{redisHost}:{redisConfig.Port}" },
                            Password = redisConfig.Password,
                            ChannelPrefix = RedisChannel.Literal("StreamKey")
                        };
                    });

                builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect($"{redisHost}:{redisConfig.Port},password={redisConfig.Password}"));
            }
        }

        public void AddDefaultAuthorizationData()
        {
            var authorization = builder.Configuration.GetSection("Authorization");
            if (authorization.Exists() && !string.IsNullOrEmpty(authorization.Value))
            {
                ApplicationConstants.DefaultAuthorization = authorization.Value;
            }

            var deviceId = builder.Configuration.GetSection("DeviceId");
            if (deviceId.Exists() && !string.IsNullOrEmpty(deviceId.Value))
            {
                ApplicationConstants.DefaultDeviceId = deviceId.Value;
            }
        }
    }
}
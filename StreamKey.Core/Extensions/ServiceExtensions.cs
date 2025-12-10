using System.Net.Http.Headers;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;
using StackExchange.Redis;
using StreamKey.Core.Abstractions;
using StreamKey.Core.BackgroundServices;
using StreamKey.Core.Messaging;
using StreamKey.Core.NatsListeners;
using StreamKey.Core.Observability;
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

            services.AddHostedService<ChannelsHandler>();
            services.AddHostedService<StatisticHandler>();
            services.AddHostedService<RestartHandler>();
            services.AddHostedService<TelegramHandler>();

            services.AddHostedService<ConnectionListener>();
            services.AddHostedService<ClickChannelListener>();
            services.AddHostedService<ChannelsListener>();
            services.AddHostedService<TelegramGetUserListener>();
            services.AddHostedService<CheckTelegramMemberListener>();

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
        public void AddRedis(bool isInternal)
        {
            var redisConfig = builder.Configuration
                .GetSection(nameof(RedisConfig))
                .Get<RedisConfig>();

            if (redisConfig is null) return;

            if (isInternal) redisConfig.Host = "redis";

            var redisConnectionString =
                $"{redisConfig.Host}:{redisConfig.Port},password={redisConfig.Password},abortConnect=false,keepAlive=60";

            builder.Services.AddSignalR(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(5);
                })
                .AddMessagePackProtocol()
                .AddStackExchangeRedis(redisConnectionString,
                    options =>
                    {
                        options.Configuration.ChannelPrefix = RedisChannel.Literal("StreamKey");
                    });
        }

        public void AddNats(bool isInternal)
        {
            var natsConfig = builder.Configuration
                .GetSection(nameof(NatsConfig))
                .Get<NatsConfig>();

            if (natsConfig is null) return;

            if (isInternal) natsConfig.Url = $"nats://nats:{natsConfig.Port}";

            var options = new NatsOpts()
            {
                Url = natsConfig.Url,
                Name = "StreamKey",
                AuthOpts = NatsAuthOpts.Default with
                {
                    Username = natsConfig.User,
                    Password = natsConfig.Password
                }
            };

            builder.Services.AddSingleton<INatsConnection>(_ => new NatsConnection(options));

            builder.Services.AddScoped(typeof(INatsSubscriptionProcessor<>), typeof(NatsSubscriptionProcessor<>));
            builder.Services.AddScoped(typeof(INatsRequestReplyProcessor<,>), typeof(NatsRequestReplyProcessor<,>));
            builder.Services.AddScoped(typeof(MessagePackNatsSerializer<>));
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
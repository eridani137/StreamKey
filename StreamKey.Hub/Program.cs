using DotNetEnv;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Shared.Events;
using StreamKey.Shared.Hubs;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.AddRedisBackplane(false);

builder.Services.AddSingleton<RedisPublisher>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.MapHealthChecks("/health");

app.Run();
using DotNetEnv;
using Serilog.Events;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Hub.Hubs;
using StreamKey.Shared.Abstractions;
using StreamKey.Shared.Stores;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder, LogEventLevel.Debug);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.AddRedisBackplane(false);

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IConnectionStore, RedisConnectionStore>();

var app = builder.Build();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.MapHealthChecks("/health");

app.Run();
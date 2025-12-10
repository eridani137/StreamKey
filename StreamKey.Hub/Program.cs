using DotNetEnv;
using Serilog.Events;
using StreamKey.Core.Configuration;
using StreamKey.Core.Extensions;
using StreamKey.Shared.Hubs;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

ConfigureLogging.Configure(builder, LogEventLevel.Debug);
OpenTelemetryConfiguration.Configure(builder, EnvironmentHelper.GetSeqEndpoint());

builder.Services.AddSignalR()
    .AddMessagePackProtocol();

builder.AddNats(false);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHub<BrowserExtensionHub>("/hubs/extension");

app.MapHealthChecks("/health");

app.Run();
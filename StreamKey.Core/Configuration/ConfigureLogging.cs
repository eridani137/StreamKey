using Serilog;
using Serilog.Core;
using Serilog.Events;
using StreamKey.Core.Configs;

namespace StreamKey.Core.Configuration;

public static class ConfigureLogging
{
    public static void Configure(OpenTelemetryConfig otlpConfig)
    {
        const string logs = "Logs";
        var logsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logs));

        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        const string outputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        var levelSwitch = new LoggingLevelSwitch();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(outputTemplate: outputTemplate, levelSwitch: levelSwitch)
            .WriteTo.File($"{logsPath}/.log", rollingInterval: RollingInterval.Day, outputTemplate: outputTemplate, levelSwitch: levelSwitch)
            .WriteTo.Seq(otlpConfig.Endpoint, apiKey: otlpConfig.Token)
            .CreateLogger();
    }
}
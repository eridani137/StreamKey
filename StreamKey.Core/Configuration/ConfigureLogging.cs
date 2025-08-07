using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Exceptions;
using StreamKey.Core.Extensions;

namespace StreamKey.Core.Configuration;

public static class ConfigureLogging
{
    public static void Configure(WebApplicationBuilder builder)
    {
        const string logs = "logs";
        var logsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logs));

        if (!Directory.Exists(logsPath))
        {
            Directory.CreateDirectory(logsPath);
        }

        const string outputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        var levelSwitch = new LoggingLevelSwitch();
        var seqEndpoint = EnvironmentHelper.GetSeqEndpoint();
        var seqApiKey = EnvironmentHelper.GetSeqApiKey();

        var configuration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithExceptionDetails()
            .Enrich.WithSpan()
            .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
            .WriteTo.Console(outputTemplate: outputTemplate, levelSwitch: levelSwitch)
            .WriteTo.Seq(serverUrl: seqEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
        
        Log.Logger = configuration.CreateLogger();
        
        Log.Information("Seq endpoint: {SeqEndpoint}", seqEndpoint);
        Log.Information("Seq API key: {SeqApiKey}", seqApiKey ?? "null");
        
        builder.Host.UseSerilog(Log.Logger);
    }
}
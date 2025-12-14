using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Extensions;
using StreamKey.Core.Extensions;

namespace StreamKey.Core.Configuration;

public static class ConfigureLogging
{
    public static void Configure(WebApplicationBuilder builder, LogEventLevel logEventLevel = LogEventLevel.Information)
    {
        const string outputTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        var levelSwitch = new LoggingLevelSwitch(logEventLevel);
        var seqEndpoint = EnvironmentHelper.GetSeqEndpoint();
        var seqApiKey = EnvironmentHelper.GetSeqApiKey();

        var configuration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Http.Connections", LogEventLevel.Warning)
            .MinimumLevel.Override("NATS.Client.Core.Internal", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            
            .Enrich.WithThreadId()
            
            .Enrich.WithExceptionDetails()
            
            .Enrich.WithSpan()
            
            .Enrich.WithClientIp()
            .Enrich.WithRequestBody()
            .Enrich.WithRequestQuery()
            
            .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
            .WriteTo.Console(outputTemplate: outputTemplate, levelSwitch: levelSwitch);

        if (!string.IsNullOrEmpty(seqEndpoint) && !string.IsNullOrEmpty(seqApiKey))
        {
            configuration.WriteTo.Seq(serverUrl: seqEndpoint, apiKey: seqApiKey, controlLevelSwitch: levelSwitch);
        }

        Log.Logger = configuration.CreateLogger();

        // Log.Information("Seq endpoint: {SeqEndpoint}", seqEndpoint);
        // Log.Information("Seq API key: {SeqApiKey}", seqApiKey ?? "null");

        builder.Host.UseSerilog(Log.Logger);
    }
}
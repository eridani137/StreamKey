using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StreamKey.Core.Extensions;

namespace StreamKey.Core.Configuration;

public static class OpenTelemetryConfiguration
{
    private static readonly string OtlpEndpoint = EnvironmentHelper.GetSeqEndpoint();

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{OtlpEndpoint}/ingest/otlp/v1/traces");
                        options.Protocol = OtlpExportProtocol.Grpc;
                    });
            })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{OtlpEndpoint}/ingest/otlp/v1/metrics");;
                    o.Protocol = OtlpExportProtocol.Grpc;
                });
        });
    }
}
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace StreamKey.Core.Configuration;

public static class OpenTelemetryConfiguration
{
    private static readonly Uri OtlpEndpoint = new("http://otel-collector:4317");

    public static void Configure(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                    .AddHttpClientInstrumentation(options => options.RecordException = true)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = OtlpEndpoint;
                        options.Protocol = OtlpExportProtocol.Grpc;
                        options.TimeoutMilliseconds = 10_000;
                    });
            });
        // .WithMetrics(metrics =>
        // {
        //     metrics.AddAspNetCoreInstrumentation()
        //         .AddHttpClientInstrumentation()
        //         .AddRuntimeInstrumentation()
        //         .AddProcessInstrumentation()
        //         .AddOtlpExporter(o =>
        //         {
        //             o.Endpoint = OtlpEndpoint;
        //             o.Protocol = OtlpExportProtocol.Grpc;
        //             o.TimeoutMilliseconds = 10_000;
        //         });
        // });
    }
}
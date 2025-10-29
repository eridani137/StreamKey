using System.Diagnostics;
using System.Net;
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
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext => !httpContext.Request.Path.StartsWithSegments("/channels");
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.FilterHttpRequestMessage = (httpRequestMessage) => 
                            !httpRequestMessage.RequestUri?.Host.Contains("usher.ttvnw.net") == true;
                        
                        options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                        {
                            if (httpRequestMessage.RequestUri?.Host.Contains("usher.ttvnw.net") == true)
                            {
                                activity.SetTag("service.context", "stream_check");
                            }
                        };
                        
                        options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                        {
                            if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound && 
                                activity.GetTagItem("service.context")?.ToString() == "stream_check")
                            {
                                activity.SetTag("expected_error", "true");
                                activity.SetStatus(ActivityStatusCode.Ok, "Expected stream not found");
                            }
                        };
                    })
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddNpgsql()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{OtlpEndpoint}/ingest/otlp/v1/traces");
                        options.Protocol = OtlpExportProtocol.HttpProtobuf;
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
                    o.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
        });
    }
}
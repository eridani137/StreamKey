using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StreamKey.Core.Configuration.Observability;

namespace StreamKey.Core.Configuration;

public static class OpenTelemetryConfiguration
{
    public static void Configure(WebApplicationBuilder builder, string otlpEndpoint)
    {
        var excludedPaths = builder.Configuration.GetSection("OpenTelemetry:ExcludedPaths").Get<string[]>() ?? [];
        
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler<IgnoreSignalRSampler>()
                    .AddProcessor(new IgnorePathProcessor("/health", "/metrics", "/hubs"))
                    .AddProcessor(new ErrorOnlyProcessor())
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.Filter = httpContext => 
                        {
                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !excludedPaths.Any(excludedPath => path.StartsWith(excludedPath));
                        };
                        
                        options.EnrichWithHttpRequest = (activity, httpRequest) =>
                        {
                            activity.SetTag("http.request.query_string", httpRequest.QueryString.Value);
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.FilterHttpRequestMessage = msg =>
                        {
                            var path = msg.RequestUri?.AbsolutePath;
                            return path == null ||
                                   !excludedPaths.Any(p =>
                                       path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
                        };
                    })
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri($"{otlpEndpoint}/ingest/otlp/v1/traces");
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
                        o.Endpoint = new Uri($"{otlpEndpoint}/ingest/otlp/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                    });
            });
    }
}
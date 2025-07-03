using System.Net;
using System.Net.Http.Headers;
using Carter;
using Serilog;
using StreamKey.Api;
using StreamKey.Application;
using StreamKey.Application.Interfaces;
using StreamKey.Application.Services;
using StreamKey.Core.Configs;
using StreamKey.Core.Configuration;


try
{
    var builder = WebApplication.CreateBuilder(args);

    var otlpConfig = builder.Configuration.GetSection(nameof(OpenTelemetryConfig)).Get<OpenTelemetryConfig>();
    if (otlpConfig is null)
    {
        throw new ApplicationException("Нужно указать настройки OpenTelemetry");
    }

    ConfigureLogging.Configure(otlpConfig);
    OpenTelemetryConfiguration.Configure(builder, otlpConfig);

    builder.Services.AddApplication();

    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddCarter();

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    builder.Services.AddHttpClient<IUsherService, UsherService>((_, client) =>
    {
        client.BaseAddress = StaticData.UsherUrl;
        client.DefaultRequestHeaders.Referrer = new Uri(StaticData.SiteUrl);
        foreach (var header in StaticData.Headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });

    CorsConfiguration.ConfigureCors(builder);

    var app = builder.Build();

    // app.UseSerilogRequestLogging();

    app.UseCors("AllowAll");

    app.UseExceptionHandler();
    app.UseHttpsRedirection();
    app.MapCarter();
    app.MapOpenApi("/openapi/v1.json");

    app.Run();
}
catch (Exception e)
{
    Log.ForContext<Program>().Fatal(e, "Ошибка инициализации сервиса");
}
finally
{
    await Log.CloseAndFlushAsync();
}
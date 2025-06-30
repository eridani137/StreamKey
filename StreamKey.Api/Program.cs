using Carter;
using Serilog;
using StreamKey.Application;
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

    builder.Host.UseSerilog(Log.Logger);

    builder.Services.AddOpenApi();
    builder.Services.AddCarter();

    builder.Services.AddApplication();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.MapCarter();

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
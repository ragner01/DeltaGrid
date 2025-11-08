using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddHttpClientInstrumentation())
    .WithMetrics(m => m.AddRuntimeInstrumentation());

builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.Services.AddSingleton<IOC.Ingestion.TagRegistry>();
builder.Services.AddHostedService<IOC.Ingestion.TagRegistryReloader>();

builder.Services.AddSingleton<IOC.Ingestion.IPublisher, IOC.Ingestion.EventHubsPublisher>();

builder.Services.AddHostedService<IOC.Ingestion.ConnectorHostService>();

var host = builder.Build();

await host.RunAsync();

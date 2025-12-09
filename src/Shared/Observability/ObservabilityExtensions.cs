using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Elastic.CommonSchema.Serilog;
using Serilog.Sinks.Elasticsearch;

#pragma warning disable CA1716 // Identifiers should not match keywords
namespace Shared.Observability;

public static class ObservabilityExtensions
{
    public static IServiceCollection AddObservability(this IServiceCollection services, string serviceName, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // ActivitySource is managed by DI container, which will handle disposal
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by DI container
        var activitySource = new ActivitySource(serviceName);
        services.AddSingleton(activitySource);
#pragma warning restore CA2000

        services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, serviceName, configuration))
            .WithTracing(builder =>
            {
                builder.AddSource(serviceName);
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddEntityFrameworkCoreInstrumentation();

                var endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                }
            })
            .WithMetrics(builder =>
            {
                builder.AddRuntimeInstrumentation();
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddPrometheusExporter();
                var endpoint = configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    builder.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                }
            });

        return services;
    }

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("service", serviceName)
            .Enrich.With(new SensitiveDataRedactor())
            .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);

        var openSearchUri = builder.Configuration["Serilog:OpenSearch:Uri"];
        if (!string.IsNullOrWhiteSpace(openSearchUri))
        {
            loggerConfig = loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(openSearchUri))
            {
                AutoRegisterTemplate = false,
                CustomFormatter = new EcsTextFormatter(),
                IndexFormat = builder.Configuration["Serilog:OpenSearch:IndexFormat"] ?? "mynewlittlebank-logs-{0:yyyy.MM.dd}",
                NumberOfShards = 1
            });
        }

        var logger = loggerConfig.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(logger, dispose: true);

        return builder;
    }

    private static ResourceBuilder ConfigureResource(ResourceBuilder resourceBuilder, string serviceName, IConfiguration configuration)
    {
        var environmentName = configuration["OpenTelemetry:ServiceEnvironment"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "production";
        var version = configuration["OpenTelemetry:ServiceVersion"]
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        resourceBuilder.AddService(
            serviceName: serviceName,
            serviceVersion: version,
            serviceInstanceId: Environment.MachineName);

        resourceBuilder.AddAttributes(new[]
        {
            new KeyValuePair<string, object?>("deployment.environment", environmentName)
        });

        return resourceBuilder;
    }

    private sealed class SensitiveDataRedactor : ILogEventEnricher
    {
        private static readonly string[] SensitiveKeys = new[]
        {
            "password",
            "secret",
            "token",
            "cpf",
            "document",
            "email"
        };

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            foreach (var property in logEvent.Properties.ToArray())
            {
                if (!IsSensitive(property.Key))
                {
                    continue;
                }

                logEvent.RemovePropertyIfPresent(property.Key);
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(property.Key, "[REDACTED]"));
            }
        }

        public static bool IsSensitive(string key)
        {
            return Array.Exists(SensitiveKeys, candidate => key.Contains(candidate, StringComparison.OrdinalIgnoreCase));
        }
    }
}

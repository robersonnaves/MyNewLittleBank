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
using OpenTelemetry.Logs;
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
    public static IHostApplicationBuilder AddObservability(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 1. Configurar Resource Builder (Service Name, Version, etc)
        var resourceBuilder = ConfigureResource(builder.Configuration);

        // 2. Adicionar Logging OpenTelemetry
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.SetResourceBuilder(resourceBuilder);

            if (builder.Configuration.GetValue("OpenTelemetry:Logging:Enabled", false))
            {
                var endpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint))
                {
                    logging.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                }
            }
        });

        // 3. Adicionar Tracing e Metrics
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing =>
            {
                tracing.SetResourceBuilder(resourceBuilder);
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                tracing.AddEntityFrameworkCoreInstrumentation();
                
                // Adicionar Source do próprio serviço para spans manuais
                var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] 
                                 ?? builder.Environment.ApplicationName;
                tracing.AddSource(serviceName);
                
                // Adicionar Source do Rebus para tracing de mensagens
                tracing.AddSource("Rebus.Messaging");

                if (builder.Configuration.GetValue("OpenTelemetry:Tracing:Enabled", false))
                {
                    var endpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        tracing.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                    }
                }
                
                // Configurar Sampling
                var sampling = builder.Configuration["OpenTelemetry:Tracing:Sampling"];
                if (string.Equals(sampling, "AlwaysOn", StringComparison.OrdinalIgnoreCase))
                {
                    tracing.SetSampler(new AlwaysOnSampler());
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(resourceBuilder);
                metrics.AddRuntimeInstrumentation();
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();

                // Adicionar Meter do próprio serviço para métricas manuais
                var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] 
                                 ?? builder.Environment.ApplicationName;
                metrics.AddMeter(serviceName);

                // Adicionar PrometheusExporter para /metrics endpoint
                metrics.AddPrometheusExporter();

                if (builder.Configuration.GetValue("OpenTelemetry:Metrics:Enabled", false))
                {
                    var endpoint = builder.Configuration["OpenTelemetry:Otlp:Endpoint"];
                    if (!string.IsNullOrWhiteSpace(endpoint))
                    {
                        metrics.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                    }
                }
            });

        // Registrar ActivitySource para injeção de dependência se necessário
        // Embora seja melhor usar ApplicationMetrics ou ActivitySource estático,
        // manter registro no DI pode ajudar em alguns cenários.
        var appServiceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? builder.Environment.ApplicationName;
#pragma warning disable CA2000 // ActivitySource is registered as singleton and will be disposed by the DI container
        var activitySource = new ActivitySource(appServiceName);
#pragma warning restore CA2000
        builder.Services.AddSingleton(activitySource);

        return builder;
    }

    private static ResourceBuilder ConfigureResource(IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "Unknown-Service";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var environment = configuration["OpenTelemetry:Environment"] ?? "Production";
        var role = configuration["OpenTelemetry:Role"] ?? "Unknown";

        return ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment,
                ["service.role"] = role
            });
    }

    public static IHostApplicationBuilder AddSerilogLogging(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Se OpenTelemetry Logging já está ativo, Serilog pode ser redundante ou complementar.
        // Vamos manter a configuração existente de Serilog mas ler da config nova se precisar.
        // Simplificação: Ler ServiceName da config
        var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "Unknown";

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


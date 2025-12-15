using Domain.Interfaces;
using Infra.Database;
using Infra.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Services.Card;
using Shared.Health;
using Shared.Observability;
using UseCases.Transactions;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "services.card";

builder.AddSerilogLogging();
builder.AddObservability();
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddRabbitMessaging(builder.Configuration);

builder.Services.AddScoped<IProcessTransactionsHandler, ProcessTransactionsHandler>();
builder.Services.AddScoped<IMessageConsumer, CardTransactionReceiver>();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint();

await app.RunAsync().ConfigureAwait(false);

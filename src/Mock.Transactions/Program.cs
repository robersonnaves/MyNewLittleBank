using Infra.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Mock.Transactions;
using Shared.Health;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "mock.transactions";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

builder.Configuration.AddCommandLine(args);

builder.Services
    .AddOptions<MockTransactionsSettings>()
    .Bind(builder.Configuration.GetSection("MockTransactions"))
    .Validate(settings => settings.MessagesPerSecond >= 0, "MessagesPerSecond must be non-negative.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.TransactionType), "TransactionType is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<TransactionDtoGeneratorFactory>();
builder.Services.AddRabbitMessaging(builder.Configuration);
builder.Services.AddHostedService<MockTransactionsWorker>();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint();

await app.RunAsync().ConfigureAwait(false);

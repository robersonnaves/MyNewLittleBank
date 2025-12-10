using Infra.Message;
using Infra.Message.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Services.Heartbeats;
using Shared.Health;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "services.heartbeats";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

builder.Services.Configure<HeartbeatOptions>(builder.Configuration.GetSection("Heartbeat"));
builder.Services.AddRabbitMessaging(builder.Configuration);
builder.Services.AddHostedService<HeartbeatPublisher>();
builder.Services.AddHostedService<HeartbeatConsumer>();

var app = builder.Build();

// Ensure RabbitMQ topology before starting
using (var scope = app.Services.CreateScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<IRabbitTopologyBootstrapper>();
    await bootstrapper.EnsureTopologyAsync().ConfigureAwait(false);
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint();

await app.RunAsync().ConfigureAwait(false);

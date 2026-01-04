using Domain.Interfaces;
using Infra.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Services.Heartbeats;
using Shared.Health;
using Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

builder.Services.Configure<HeartbeatOptions>(builder.Configuration.GetSection("Heartbeat"));
builder.Services.AddRabbitMessaging(builder.Configuration);
builder.Services.AddHostedService<HeartbeatPublisher>();
builder.Services.AddScoped<IMessageConsumer, HeartbeatConsumer>();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

await app.RunAsync().ConfigureAwait(false);

using API.Endpoints;
using Infra.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Shared.Health;
using Shared.Observability;
using UseCases.Accounts;
using UseCases.Clients;

var builder = WebApplication.CreateBuilder(args);
const string serviceName = "api";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICreateClientHandler, CreateClientHandler>();
builder.Services.AddScoped<IGetClientHandler, GetClientHandler>();
builder.Services.AddScoped<IGetAccountHandler, GetAccountHandler>();
builder.Services.AddScoped<IGetAccountBalanceHandler, GetAccountBalanceHandler>();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapClientEndpoints();
app.MapAccountEndpoints();

await app.RunAsync().ConfigureAwait(false);

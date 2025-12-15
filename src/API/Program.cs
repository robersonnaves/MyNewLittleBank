using API.Endpoints;
using Infra.Database;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Shared.Health;
using Shared.Observability;
using System.IO;
using System.Reflection;
using UseCases.Accounts;
using UseCases.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.AddSerilogLogging();
builder.AddObservability();
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MyNewLittleBank API",
        Version = "v1",
        Description = "HTTP API for managing clients and bank accounts.",
        Contact = new OpenApiContact
        {
            Name = "MyNewLittleBank Team",
            Email = "api@mynewlittlebank.local"
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddScoped<ICreateClientHandler, CreateClientHandler>();
builder.Services.AddScoped<IGetClientHandler, GetClientHandler>();
builder.Services.AddScoped<IGetClientByCpfHandler, GetClientByCpfHandler>();
builder.Services.AddScoped<IOpenAccountHandler, OpenAccountHandler>();
builder.Services.AddScoped<IGetAccountHandler, GetAccountHandler>();
builder.Services.AddScoped<IGetAccountBalanceHandler, GetAccountBalanceHandler>();

var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapPrometheusScrapingEndpoint();

var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Environment.IsStaging()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");

if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyNewLittleBank API v1");
    });
}

app.MapClientEndpoints();
app.MapAccountEndpoints();

await app.RunAsync().ConfigureAwait(false);

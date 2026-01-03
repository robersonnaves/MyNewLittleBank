using Infra.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Mock.Transactions;
using Shared.Health;
using Shared.Observability;
using Microsoft.Extensions.Options;

using MyNewLittleBank.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogLogging();
builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

builder.Configuration.AddCommandLine(args);

builder.Services
    .AddOptions<MockTransactionsSettings>()
    .Bind(builder.Configuration.GetSection("MockTransactions"))
    .Validate(settings => settings.MessagesPerSecond >= 0, "MessagesPerSecond must be non-negative.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.TransactionType), "TransactionType is required.")
    .Validate(settings => !string.IsNullOrWhiteSpace(settings.ApiBaseUrl), "ApiBaseUrl is required.")
    .Validate(settings => !settings.Seed.Enabled || settings.Seed.Clients == 10, "Seed.Clients must be exactly 10 when seed is enabled.")
    .Validate(settings => !settings.Seed.Enabled || (settings.Seed.MinAccountsPerClient >= 1 && settings.Seed.MinAccountsPerClient <= settings.Seed.MaxAccountsPerClient), "Seed accounts per client min must be >= 1 and <= max when seed is enabled.")
    .Validate(settings => !settings.Seed.Enabled || settings.Seed.MaxAccountsPerClient <= 3, "Seed accounts per client max must be <= 3 when seed is enabled.")
    .Validate(settings => settings.Seed.InitialBalance >= 0, "Seed.InitialBalance must be non-negative.")
    .ValidateOnStart();

builder.Services.AddSingleton<SeededAccountProvider>();
builder.Services.AddSingleton<TransactionDtoGeneratorFactory>();
builder.Services.AddHttpClient<ApiSeedService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<MockTransactionsSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl))
    {
        client.BaseAddress = new Uri(options.ApiBaseUrl);
    }
});
builder.Services.AddRabbitMessaging(builder.Configuration);
builder.Services.AddHostedService<MockTransactionsWorker>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

await app.RunAsync().ConfigureAwait(false);

using Infra.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mock.Transactions;
using Shared.Observability;

var builder = Host.CreateApplicationBuilder(args);
const string serviceName = "mock.transactions";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);

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
await app.RunAsync().ConfigureAwait(false);

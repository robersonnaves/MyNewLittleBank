using Domain.Interfaces;
using Infra.Database;
using Infra.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Pix;
using Shared.Observability;
using UseCases.Transactions;

var builder = Host.CreateApplicationBuilder(args);
const string serviceName = "services.pix";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);
builder.Services.AddDatabaseInfrastructure(builder.Configuration);
builder.Services.AddRabbitMessaging(builder.Configuration);

builder.Services.AddScoped<IProcessTransactionsHandler, ProcessTransactionsHandler>();
builder.Services.AddHostedService<PixTransactionReceiver>();

var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);

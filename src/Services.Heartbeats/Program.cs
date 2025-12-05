using Infra.Message;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services.Heartbeats;
using Shared.Observability;

var builder = Host.CreateApplicationBuilder(args);
const string serviceName = "services.heartbeats";

builder.AddSerilogLogging(serviceName);
builder.Services.AddObservability(serviceName, builder.Configuration);

builder.Services.Configure<HeartbeatOptions>(builder.Configuration.GetSection("Heartbeat"));
builder.Services.AddRabbitMessaging(builder.Configuration);
builder.Services.AddHostedService<HeartbeatPublisher>();
builder.Services.AddHostedService<HeartbeatConsumer>();

var app = builder.Build();
await app.RunAsync().ConfigureAwait(false);

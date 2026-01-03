using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "16-alpine")
    .WithPgAdmin()
    .WithDataVolume();

var db = postgres.AddDatabase("mynewlittlebank");

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithArgs("--config=/etc/otel-collector-config.yaml")
    .WithBindMount("../infra/otel-collector-config.yaml", "/etc/otel-collector-config.yaml", isReadOnly: true)
    .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithHttpEndpoint(port: 8889, targetPort: 8889, name: "prometheus-exporter")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

var api = builder.AddProject<Projects.API>("api")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

builder.AddProject<Projects.Mock_Transactions>("mock-transactions")
    .WithReference(rabbitmq)
    .WithReference(api)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

builder.AddProject<Projects.Services_Card>("services-card")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

builder.AddProject<Projects.Services_Heartbeats>("services-heartbeats")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

builder.AddProject<Projects.Services_Money>("services-money")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

builder.AddProject<Projects.Services_Pix>("services-pix")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"));

await builder.Build().RunAsync().ConfigureAwait(false);

# Observability Architecture - MyNewLittleBank

## 📊 Overview

Simplified observability stack using **Aspire Dashboard** as the single destination for all telemetry data (logs, traces, and metrics).

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────┐
│                   .NET Microservices                     │
│  (API, Services.Pix, Services.Card, Services.Money,      │
│   Services.Heartbeats, Mock.Transactions)                │
└────────────────────┬─────────────────────────────────────┘
                     │ OTLP (gRPC:4317 / HTTP:4318)
                     ↓
┌──────────────────────────────────────────────────────────┐
│           OpenTelemetry Collector                        │
│  - Receives: OTLP (traces, metrics, logs)                │
│  - Processes: batch, memory_limiter, attributes          │
│  - Exports: Aspire Dashboard only                        │
└────────────────────┬─────────────────────────────────────┘
                     │ OTLP (gRPC:18888)
                     ↓
┌──────────────────────────────────────────────────────────┐
│              Aspire Dashboard                            │
│  - Web UI: http://localhost:15000                        │
│  - Structured Logs                                       │
│  - Distributed Traces                                    │
│  - Metrics (real-time)                                   │
│  - Resources (services overview)                         │
└──────────────────────────────────────────────────────────┘
```

## 🎯 Design Decisions

### Single Destination Strategy
- **All telemetry** flows through OTel Collector to Aspire Dashboard only
- **Removed**: Jaeger and Prometheus (previously unused)
- **Benefits**: 
  - Simpler configuration
  - Less resource overhead
  - Unified interface
  - Easier maintenance

### Configuration Pattern
Every .NET service uses the same environment variables:
```yaml
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
  - OTEL_SERVICE_NAME=<service-name>
  - OTEL_RESOURCE_ATTRIBUTES=deployment.environment=local
```

## 📦 Components

### 1. .NET Services (6 total)
Each service is instrumented with OpenTelemetry SDK and configured to:
- Export telemetry via OTLP protocol
- Include service name and environment attributes
- Use shared configuration from `MyNewLittleBank.ServiceDefaults`

**Services**:
- `api` - API Gateway
- `services-pix` - PIX transactions
- `services-card` - Card operations
- `services-money` - Money transfers
- `services-heartbeats` - Health checks
- `mock-transactions` - Transaction simulator

### 2. OpenTelemetry Collector
**Image**: `otel/opentelemetry-collector-contrib:0.112.0`

**Configuration** (`otel-collector-config.yaml`):
- **Receivers**: OTLP (gRPC on 4317, HTTP on 4318)
- **Processors**: 
  - `batch` - Batches telemetry for efficiency
  - `memory_limiter` - Prevents OOM (512MB limit)
  - `attributes` - Adds deployment.environment=local
- **Exporters**: 
  - `otlp/aspire` - Sends to Aspire Dashboard
  - `debug` - Console logging for troubleshooting

**Restart Policy**: `on-failure` to handle Aspire startup timing

### 3. Aspire Dashboard
**Image**: `mcr.microsoft.com/dotnet/aspire-dashboard:9.0.1`

**Ports**:
- `15000` - Web UI (mapped from internal 8080)
- `18888` - OTLP gRPC receiver
- `18890` - OTLP HTTP receiver (not exposed)

**Features**:
- Real-time telemetry visualization
- Distributed tracing with service maps
- Structured log viewing with filtering
- Metrics explorer with live updates
- Resource/service health overview

**Data Persistence**:
- Volume `aspire_data_protection` persists ASP.NET Data Protection keys
- Prevents browser cache invalidation on restarts

## 🔧 Key Configurations

### Service Configuration (`ServiceDefaults/Extensions.cs`)
```csharp
using OpenTelemetry.Resources;

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName)
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = 
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? "Development"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation())
    .WithLogging(logging => logging
        .AddOtlpExporter());

// OTLP Exporter configured via environment variables
builder.Services.AddOpenTelemetryExporters();
```

### OTel Collector Pipeline
```yaml
service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch, attributes]
      exporters: [otlp/aspire, debug]
    
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch, attributes]
      exporters: [otlp/aspire, debug]
    
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch, attributes]
      exporters: [otlp/aspire, debug]
```

## 🚀 Usage

### Accessing Telemetry
1. Open **http://localhost:15000** in your browser
2. Navigate between views:
   - **Structured Logs** - Filter by service, log level, timestamp
   - **Traces** - View distributed traces with timing breakdown
   - **Metrics** - Monitor request rates, durations, errors
   - **Resources** - See all services and their health

### Verifying Data Flow

**Check OTel Collector is processing**:
```bash
podman logs otel-collector | tail -50
```
Should show: `Traces`, `Metrics`, `LogRecords` being exported

**Check service configuration**:
```bash
podman exec api printenv | grep OTEL
```
Should show:
```
OTEL_SERVICE_NAME=api
OTEL_RESOURCE_ATTRIBUTES=deployment.environment=local
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

**Verify containers**:
```bash
podman ps --format "table {{.Names}}\t{{.Status}}"
```
All 13 containers should be `Up` or `Up (healthy)`

## 🐛 Troubleshooting

### Telemetry Not Appearing in Aspire

1. **Check OTel Collector logs**:
   ```bash
   podman logs otel-collector --tail 100
   ```
   Look for connection errors or export failures

2. **Verify Aspire is ready**:
   ```bash
   podman logs apphost | grep "listening"
   ```
   Should show listening on ports 8080 and 18888

3. **Restart OTel Collector** (if Aspire restarted):
   ```bash
   podman restart otel-collector
   ```
   The `restart: on-failure` policy should handle this automatically

### Service Names Show as "unknown_service:dotnet"

Check two things:
1. Environment variable is set: `podman exec <service> printenv | grep OTEL_SERVICE_NAME`
2. ResourceBuilder is configured in `Extensions.cs` with `AddService()`

### Data Protection Key Errors in Browser

This is cosmetic and doesn't affect telemetry. Clear browser cache or wait for volume to persist keys across restarts.

## 📈 Performance Characteristics

### Resource Usage (per component)
- **OTel Collector**: ~50-100MB RAM, minimal CPU
- **Aspire Dashboard**: ~100-150MB RAM, minimal CPU
- **Per Service**: +20-30MB RAM for OTel SDK instrumentation

### Latency Impact
- OTLP export is **non-blocking** (async)
- Typical overhead: <1ms per request
- Batching reduces network calls

### Data Retention
- **Aspire Dashboard**: In-memory only (no persistence)
- Data lost on container restart
- For production, consider external storage (e.g., Azure Monitor, Elasticsearch)

## 🔒 Security Notes

### Current Configuration (Development)
- `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true` - No TLS between services
- `DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` - No authentication
- All ports exposed to host machine

### Production Recommendations
- Enable TLS for OTLP connections
- Add authentication to Aspire Dashboard
- Use API keys for OTel Collector
- Restrict port exposure
- Consider managed observability services

## 📚 Additional Resources

- [Aspire Dashboard Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [OTel Collector Configuration](https://opentelemetry.io/docs/collector/configuration/)

---

**Last Updated**: 2026-01-03  
**Status**: ✅ Fully Operational  
**Containers**: 13 active (Jaeger and Prometheus removed)

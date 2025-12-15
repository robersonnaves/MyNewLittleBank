# Observability Stack - MyNewLittleBank

## Arquitetura de Observabilidade

Este projeto utiliza uma stack completa de observabilidade baseada no **OpenTelemetry Collector** como ponto central de coleta e distribuição de telemetria.

```
┌─────────────────────────────────────────────────────────────────┐
│                       Aplicações .NET                            │
│  (API, Mock.Transactions, Services.Card/Money/Pix/Heartbeats)  │
│                                                                  │
│  - Logs (Serilog + OpenTelemetry)                              │
│  - Traces (OpenTelemetry)                                       │
│  - Metrics (OpenTelemetry + Prometheus Exporter)               │
└────────────────────┬────────────────────────────────────────────┘
                     │
                     │ OTLP (gRPC/HTTP)
                     │ Port 4317/4318
                     ▼
┌─────────────────────────────────────────────────────────────────┐
│              OpenTelemetry Collector                             │
│                                                                  │
│  Receivers:  OTLP (gRPC + HTTP)                                 │
│  Processors: batch, memory_limiter, attributes                  │
│  Exporters:                                                      │
│    - Traces  → Jaeger (OTLP)                                    │
│    - Metrics → Prometheus (scrape endpoint :8889)               │
│    - Logs    → Console (debug)                                  │
└─────┬───────────────────────────┬───────────────────────────────┘
      │                           │
      │ OTLP :4317                │ Prometheus scrape :8889
      ▼                           ▼
┌─────────────┐           ┌──────────────┐
│   Jaeger    │           │  Prometheus  │
│   :16686    │           │    :9090     │
│             │           │              │
│  UI Traces  │           │  UI Metrics  │
└─────────────┘           └──────────────┘
```

## Componentes

### 1. OpenTelemetry Collector (`otel-collector`)
- **Imagem:** `otel/opentelemetry-collector-contrib:0.112.0`
- **Portas:**
  - `4317`: OTLP gRPC receiver (usado pelas aplicações)
  - `4318`: OTLP HTTP receiver
  - `8889`: Prometheus metrics exporter
  - `8888`: Collector internal metrics
  - `13133`: Health check endpoint
  - `55679`: zPages (debug)

**Configuração:** `otel-collector-config.yaml`

### 2. Jaeger (`jaeger`)
- **Imagem:** `jaegertracing/all-in-one:1.58`
- **Porta:** `16686` (UI)
- **Função:** Visualização de traces distribuídos
- **Acesso:** http://localhost:16686

### 3. Prometheus (`prometheus`)
- **Imagem:** `prom/prometheus:v2.54.1`
- **Porta:** `9090` (UI)
- **Função:** Armazenamento e query de métricas
- **Acesso:** http://localhost:9090
- **Scrape targets:**
  - Prometheus interno (:9090)
  - Jaeger metrics (:14269)
  - OTel Collector internal metrics (:8888)
  - Application metrics via OTel Collector (:8889)

### 4. OpenSearch (`opensearch`)
- **Imagem:** `opensearchproject/opensearch:2.15.0`
- **Porta:** `9200`
- **Função:** Armazenamento de logs via Serilog
- **Acesso:** http://localhost:9200

## Fluxo de Dados

### Traces (Distributed Tracing)
1. Aplicações .NET geram spans usando OpenTelemetry SDK
2. Spans são enviados via OTLP para o Collector (`:4317`)
3. Collector processa (batch, attributes) e envia para Jaeger
4. Visualização no Jaeger UI (`:16686`)

**Instrumentação automática:**
- ASP.NET Core (HTTP requests)
- HttpClient (HTTP calls)
- Entity Framework Core (database queries)

### Metrics
1. Aplicações coletam métricas via OpenTelemetry SDK
2. Métricas são enviadas via OTLP para o Collector (`:4317`)
3. Collector expõe endpoint Prometheus (`:8889`)
4. Prometheus faz scrape do endpoint a cada 5 segundos
5. Visualização no Prometheus UI (`:9090`)

**Métricas disponíveis:**
- Runtime (.NET GC, memory, threads)
- ASP.NET Core (HTTP requests, response times)
- HttpClient (outgoing requests)
- Custom metrics (via Meter API)

### Logs
1. Serilog enriquece logs com contexto
2. Logs vão para Console (stdout) e OpenSearch
3. OpenTelemetry Logging SDK envia structured logs para Collector
4. Collector exporta para console (debug)

**Enriquecimento de logs:**
- Service name
- Trace/span IDs (correlation)
- Redação de dados sensíveis (CPF, email, password, etc.)

## Configuração nas Aplicações

Todas as aplicações já estão configuradas via `appsettings.json`:

```json
{
  "OpenTelemetry": {
    "ServiceName": "MyNewLittleBank-API",
    "ServiceVersion": "1.0.0",
    "Environment": "Development",
    "Role": "API",
    "Otlp": {
      "Endpoint": "http://otel-collector:4317"
    },
    "Tracing": {
      "Enabled": true,
      "Sampling": "AlwaysOn"
    },
    "Metrics": {
      "Enabled": true
    },
    "Logging": {
      "Enabled": true
    }
  }
}
```

## Como Usar

### 1. Iniciar a Stack
```bash
cd infra
podman-compose up -d
```

### 2. Acessar as UIs

- **Jaeger (Traces):** http://localhost:16686
  - Selecione o serviço (ex: `MyNewLittleBank-API`)
  - Visualize traces de requisições HTTP, queries SQL, etc.

- **Prometheus (Metrics):** http://localhost:9090
  - Consultas PromQL
  - Exemplo: `rate(http_server_duration_milliseconds_count[5m])`

- **OTel Collector zPages:** http://localhost:55679
  - Debug de pipelines e receivers

### 3. Verificar Health
```bash
# Collector health
curl http://localhost:13133

# Métricas das aplicações via Collector
curl http://localhost:8889/metrics

# Métricas internas do Collector
curl http://localhost:8888/metrics
```

## Consultas Úteis

### Prometheus (PromQL)

**Request rate por serviço:**
```promql
rate(http_server_duration_milliseconds_count[5m])
```

**P95 latency:**
```promql
histogram_quantile(0.95, rate(http_server_duration_milliseconds_bucket[5m]))
```

**Garbage Collection:**
```promql
rate(process_runtime_dotnet_gc_collections_count_total[5m])
```

**Memory usage:**
```promql
process_runtime_dotnet_gc_heap_size_bytes
```

### Jaeger

1. Selecione o serviço no dropdown
2. Defina o período de tempo
3. Filtros úteis:
   - `http.status_code=500` (erros)
   - `duration > 1s` (requests lentos)
   - `db.system=postgresql` (queries de banco)

## Troubleshooting

### Aplicações não enviam telemetria

1. Verificar logs do Collector:
```bash
podman logs otel-collector
```

2. Verificar configuração OTLP nas aplicações:
```bash
podman logs api | grep -i "otlp\|opentelemetry"
```

3. Testar conectividade:
```bash
podman exec api curl -v http://otel-collector:4317
```

### Métricas não aparecem no Prometheus

1. Verificar targets no Prometheus:
   - http://localhost:9090/targets
   - Status deve ser "UP"

2. Verificar endpoint do Collector:
```bash
curl http://localhost:8889/metrics | grep mynewlittlebank
```

### Traces não aparecem no Jaeger

1. Verificar pipeline de traces no Collector:
   - http://localhost:55679/debug/tracez

2. Verificar se Jaeger está recebendo:
```bash
podman logs jaeger | grep -i "span"
```

## Customização

### Adicionar novo exporter

Edite `otel-collector-config.yaml`:

```yaml
exporters:
  # Exemplo: enviar para Grafana Cloud
  otlp/grafana:
    endpoint: tempo.grafana.net:443
    headers:
      authorization: Basic <base64-token>

service:
  pipelines:
    traces:
      exporters: [otlp/jaeger, otlp/grafana]
```

### Ajustar sampling

Em `appsettings.json` das aplicações:

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "Sampling": "ParentBased"  // ou "TraceIdRatioBased"
    }
  }
}
```

### Adicionar processadores

Em `otel-collector-config.yaml`:

```yaml
processors:
  # Filtrar spans por atributos
  filter/traces:
    traces:
      span:
        - 'attributes["http.target"] == "/health"'

service:
  pipelines:
    traces:
      processors: [memory_limiter, batch, filter/traces, attributes]
```

## Referências

- [OpenTelemetry Docs](https://opentelemetry.io/docs/)
- [OTel Collector Config](https://opentelemetry.io/docs/collector/configuration/)
- [Jaeger Docs](https://www.jaegertracing.io/docs/)
- [Prometheus Docs](https://prometheus.io/docs/)

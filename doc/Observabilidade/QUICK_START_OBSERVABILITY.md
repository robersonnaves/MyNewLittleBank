# Quick Start: Observabilidade

Guia rápido para implementar a stack de observabilidade no MyNewLittleBank.

## 🚀 TL;DR

```bash
# 1. Backup
cp docker-compose.yml docker-compose.yml.backup

# 2. Criar estrutura
mkdir -p config/grafana/{datasources,dashboards}

# 3. Copiar configs do repo de referência
# (Ver seção Arquivos de Configuração abaixo)

# 4. Substituir Aspire Dashboard por Jaeger no docker-compose.yml
# (Ver seção Mudanças Necessárias abaixo)

# 5. Iniciar stack
podman-compose down
podman-compose up -d

# 6. Validar
curl http://localhost:16686  # Jaeger
curl http://localhost:9090   # Prometheus
curl http://localhost:3100/ready  # Loki
curl http://localhost:3000   # Grafana
```

## 📦 O Que Será Substituído

### ❌ Remover
- **Aspire Dashboard** (apphost) - Incompatível com OTel Collector

### ✅ Adicionar
- **Jaeger** - Traces distribuídos (porta 16686)
- **Prometheus** - Métricas (porta 9090) 
- **Loki** - Logs centralizados (porta 3100)
- **Grafana** - Visualização unificada (porta 3000)

### 🔄 Manter e Ajustar
- **OpenTelemetry Collector** - Ajustar configuração

## 🔧 Mudanças Necessárias

### 1. Variáveis de Ambiente (Todos os Serviços .NET)

```yaml
# ANTES
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://apphost:18888

# DEPOIS
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
  - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  - OTEL_SERVICE_NAME=api  # ou services-card, services-money, etc
  - OTEL_RESOURCE_ATTRIBUTES=deployment.environment=local
```

### 2. Collector Config (`config/otel-collector.yaml`)

```yaml
exporters:
  # ANTES: Aspire Dashboard
  otlp/aspire:
    endpoint: ${OTEL_EXPORTER_OTLP_ENDPOINT}
    
  # DEPOIS: Jaeger + Prometheus + Loki
  otlp/jaeger:
    endpoint: jaeger:4317
    tls:
      insecure: true
  
  prometheus:
    endpoint: 0.0.0.0:8889
  
  otlphttp/loki:
    endpoint: http://loki:3100/otlp

service:
  pipelines:
    traces:
      exporters: [otlp/jaeger]  # era otlp/aspire
    metrics:
      exporters: [prometheus]   # era otlp/aspire  
    logs:
      exporters: [otlphttp/loki]  # era otlp/aspire
```

### 3. Docker Compose

Substituir serviço `apphost` por:

```yaml
  jaeger:
    container_name: jaeger
    image: jaegertracing/all-in-one:latest
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686"
    networks:
      - bank-net

  prometheus:
    container_name: prometheus
    image: prom/prometheus:latest
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.retention.time=7d'
    volumes:
      - ./config/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    ports:
      - "9090:9090"
    networks:
      - bank-net

  loki:
    container_name: loki
    image: grafana/loki:latest
    command: -config.file=/etc/loki/loki.yml
    volumes:
      - ./config/loki.yml:/etc/loki/loki.yml:ro
      - loki-data:/loki
    ports:
      - "3100:3100"
    networks:
      - bank-net

  grafana:
    container_name: grafana
    image: grafana/grafana:latest
    environment:
      - GF_SECURITY_ADMIN_USER=admin
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana
      - ./config/grafana:/etc/grafana/provisioning:ro
    ports:
      - "3000:3000"
    networks:
      - bank-net

volumes:
  prometheus-data:
  loki-data:
  grafana-data:
```

## 📁 Arquivos de Configuração Necessários

Copie estes arquivos do [repo de referência](https://github.com/robersonnaves/Telemetry):

1. **`config/otel-collector.yaml`** - Configuração do collector
2. **`config/prometheus.yml`** - Scrape configs do Prometheus
3. **`config/loki.yml`** - Configuração do Loki
4. **`config/grafana/datasources/datasources.yml`** - Datasources do Grafana

## ✅ Checklist de Validação

- [ ] Jaeger UI carregando (`http://localhost:16686`)
- [ ] Prometheus com targets UP (`http://localhost:9090/targets`)
- [ ] Loki respondendo (`curl http://localhost:3100/ready`)
- [ ] Grafana acessível (`http://localhost:3000`)
- [ ] Traces aparecendo no Jaeger (após fazer requisições à API)
- [ ] Métricas aparecendo no Prometheus
- [ ] Logs aparecendo no Grafana/Loki

## 🐛 Problemas Comuns

### Jaeger não mostra traces
```bash
# Verificar logs do collector
podman logs otel-collector | grep jaeger

# Verificar logs do Jaeger
podman logs jaeger
```

### Prometheus não coleta métricas
```bash
# Testar endpoint de métricas do collector
curl http://localhost:8889/metrics

# Verificar targets no Prometheus
# Acessar: http://localhost:9090/targets
```

### Loki não recebe logs
```bash
# Verificar logs do collector
podman logs otel-collector | grep loki

# Testar API do Loki
curl "http://localhost:3100/loki/api/v1/labels"
```

## 📊 URLs de Acesso

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **Jaeger UI** | http://localhost:16686 | - |
| **Prometheus** | http://localhost:9090 | - |
| **Loki API** | http://localhost:3100 | - |
| **Grafana** | http://localhost:3000 | admin/admin |

## 🔍 Primeiras Queries

### Jaeger
1. Service: `api`
2. Operation: Qualquer
3. Lookback: Last Hour
4. Click "Find Traces"

### Prometheus
```promql
# Taxa de requisições por segundo
rate(http_server_request_duration_count[5m])

# Latência P95
histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m]))
```

### Loki (via Grafana Explore)
```logql
# Todos os logs da API
{service_name="api"}

# Apenas erros
{service_name="api"} |= "ERROR"

# Com TraceId específico
{service_name="api"} |= "TraceId=abc123"
```

## 📚 Documentação Completa

Para o plano detalhado de implementação, ver: [`PLANO_OBSERVABILIDADE.md`](./PLANO_OBSERVABILIDADE.md)

---

**Tempo estimado**: 1-2 horas  
**Dificuldade**: Média  
**Pré-requisitos**: Docker/Podman, conhecimento básico de compose

# Plano de Implementação: Stack de Observabilidade Completa

## 📋 Visão Geral

Este documento descreve o plano completo para implementar uma stack de observabilidade moderna no projeto MyNewLittleBank, baseada no repositório de referência [robersonnaves/Telemetry](https://github.com/robersonnaves/Telemetry).

### Arquitetura Proposta

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aplicações .NET                             │
│  (API, Services.Card, Services.Money, Services.Pix, etc)        │
└────────────┬─────────────────────────────────────────┬──────────┘
             │ OTLP gRPC (4317)                        │
             │ OTLP HTTP (4318)                        │
             ▼                                         │
┌────────────────────────────────────┐                │
│   OpenTelemetry Collector          │                │
│   - Recebe telemetria via OTLP     │                │
│   - Processa e enriquece dados     │                │
│   - Roteia para backends           │                │
└─────┬──────────┬──────────┬────────┘                │
      │          │          │                         │
      ▼          ▼          ▼                         │
┌─────────┐ ┌─────────┐ ┌─────────┐                  │
│ Jaeger  │ │Prometh  │ │  Loki   │◄─────────────────┘
│ (Traces)│ │(Metrics)│ │ (Logs)  │ OTLP HTTP (3100)
└────┬────┘ └────┬────┘ └────┬────┘
     │           │           │
     └───────────┴───────────┴──────────┐
                                        │
                            ┌───────────▼─────────┐
                            │      Grafana        │
                            │   (Visualização)    │
                            └─────────────────────┘
```

## 🎯 Objetivos

1. **Traces**: Rastreamento distribuído de requisições entre microserviços
2. **Métricas**: Monitoramento de performance e saúde dos serviços
3. **Logs**: Centralização e correlação de logs estruturados
4. **Visualização**: Dashboard unificado com Grafana

## 📦 Componentes da Stack

| Componente | Função | Porta Principal |
|------------|--------|----------------|
| **OpenTelemetry Collector** | Coletor central de telemetria | 4317 (gRPC), 4318 (HTTP) |
| **Jaeger** | Backend de traces distribuídos | 16686 (UI) |
| **Prometheus** | Backend de métricas time-series | 9090 (UI) |
| **Loki** | Backend de logs estruturados | 3100 (API) |
| **Grafana** | Visualização unificada | 3000 (UI) |

## 🔧 Fase 1: Preparação (30 min)

### 1.1 Backup e Limpeza

```bash
# Backup das configurações atuais
cd /home/roberson/DEV/MyNewLittleBank/infra
mkdir -p backup/$(date +%Y%m%d_%H%M%S)
cp docker-compose.yml otel-collector-config.yaml backup/$(date +%Y%m%d_%H%M%S)/

# Parar containers atuais
podman-compose down
```

### 1.2 Criar Estrutura de Diretórios

```bash
cd /home/roberson/DEV/MyNewLittleBank/infra

# Estrutura de configuração
mkdir -p config/grafana/{datasources,dashboards,provisioning}
mkdir -p data/{prometheus,loki,grafana,jaeger}

# Estrutura de arquivos
tree structure:
infra/
├── config/
│   ├── otel-collector.yaml       # Configuração do Collector
│   ├── prometheus.yml            # Configuração do Prometheus
│   ├── loki.yml                  # Configuração do Loki
│   └── grafana/
│       ├── datasources/
│       │   └── datasources.yml   # Configuração de datasources
│       └── dashboards/
│           └── dashboards.yml    # Configuração de dashboards
├── docker-compose.observability.yml  # Stack de observabilidade
└── docker-compose.yml            # Stack principal (atualizada)
```

## 🚀 Fase 2: Configuração dos Componentes (1h)

### 2.1 OpenTelemetry Collector

**Arquivo**: `config/otel-collector.yaml`

**Configuração**:
- **Receivers**: OTLP gRPC (4317) e HTTP (4318)
- **Processors**: 
  - `resourcedetection`: Detecta ambiente automaticamente
  - `resource`: Enriquece com service.name
  - `batch`: Agrupa dados para otimizar envio
  - `attributes`: Adiciona/modifica atributos
- **Exporters**:
  - `otlp` (Jaeger): Traces via gRPC
  - `prometheus`: Métricas expostas em /metrics
  - `otlphttp/loki`: Logs via HTTP

**Pipelines**:
```yaml
traces:  [otlp] → [resourcedetection, resource] → [otlp/jaeger]
metrics: [otlp] → [resourcedetection, resource] → [prometheus]
logs:    [otlp] → [resourcedetection, resource, attributes, batch] → [otlphttp/loki]
```

### 2.2 Jaeger

**Configuração**:
- Usar `jaegertracing/all-in-one:latest`
- Storage em memória (desenvolvimento)
- OTLP habilitado (`COLLECTOR_OTLP_ENABLED=true`)
- UI acessível em `http://localhost:16686`

**Recursos**:
- Busca de traces por serviço, operação, tags
- Visualização de spans hierárquicos
- Mapa de dependências entre serviços
- Análise de latências

### 2.3 Prometheus

**Arquivo**: `config/prometheus.yml`

**Configuração**:
- Scrape interval: 15s
- Retention: 7 dias
- Storage em volume Docker
- Targets:
  - `otel-collector:8889` - Métricas do collector
  - `prometheus:9090` - Auto-monitoramento

**Métricas Importantes**:
```promql
# Taxa de requisições
rate(http_server_request_duration_count[5m])

# Latência P95
histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m]))

# Erros por endpoint
sum by (http_route) (rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]))
```

### 2.4 Loki

**Arquivo**: `config/loki.yml`

**Configuração**:
- Schema: v13 com TSDB
- Storage: Filesystem persistente
- Retention: 7 dias
- Rate limiting configurado para desenvolvimento
- Suporte a structured metadata
- Pattern ingestion habilitado

**Labels Automáticos**:
- `service_name`: Nome do serviço
- `level`: Nível do log (INFO, ERROR, etc)
- `deployment_environment`: Ambiente (local, prod, etc)

### 2.5 Grafana

**Arquivo**: `config/grafana/datasources/datasources.yml`

**Datasources**:
1. **Prometheus** - Métricas
2. **Loki** - Logs
3. **Jaeger** - Traces

**Configuração**:
- Auto-provisioning de datasources
- Dashboards pré-configurados
- Credenciais: `admin/admin`

## 🔨 Fase 3: Atualização da Aplicação (1h 30min)

### 3.1 Variáveis de Ambiente

Atualizar `docker-compose.yml` para **todos os serviços .NET**:

```yaml
environment:
  # Remover (incompatível com Jaeger via Collector)
  # - OTEL_EXPORTER_OTLP_ENDPOINT=http://apphost:18888
  
  # Adicionar (correto para Collector)
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
  - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  - OTEL_SERVICE_NAME=${SERVICE_NAME}
  - OTEL_RESOURCE_ATTRIBUTES=deployment.environment=local,service.version=1.0.0
  
  # Logs estruturados
  - OTEL_LOGS_EXPORTER=otlp
  - OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE=true
```

### 3.2 Configuração do Código .NET

**Não requer alterações se já está usando**:
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MyApplication"))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation())
    .UseOtlpExporter();
```

### 3.3 Logging Estruturado

Garantir que logs usem ILogger com structured logging:

```csharp
// ✅ Correto - Structured
logger.LogInformation("Processing transaction {TransactionId} for account {AccountNumber}", 
    transactionId, accountNumber);

// ❌ Evitar - String formatting
logger.LogInformation($"Processing transaction {transactionId} for account {accountNumber}");
```

## 📊 Fase 4: Docker Compose (30 min)

### 4.1 Criar `docker-compose.observability.yml`

Stack separada para observabilidade (opcional):

```yaml
services:
  otel-collector:
    container_name: otel-collector
    image: otel/opentelemetry-collector-contrib:0.112.0
    command: ["--config=/etc/otel-collector-config.yaml"]
    volumes:
      - ./config/otel-collector.yaml:/etc/otel-collector-config.yaml:ro
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8889:8889"   # Prometheus metrics
      - "13133:13133" # Health check
    networks:
      - bank-net

  jaeger:
    container_name: jaeger
    image: jaegertracing/all-in-one:latest
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - "16686:16686"  # Jaeger UI
    networks:
      - bank-net

  prometheus:
    container_name: prometheus
    image: prom/prometheus:latest
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--storage.tsdb.retention.time=7d'
      - '--web.enable-lifecycle'
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
      - GF_USERS_ALLOW_SIGN_UP=false
    volumes:
      - grafana-data:/var/lib/grafana
      - ./config/grafana:/etc/grafana/provisioning:ro
    ports:
      - "3000:3000"
    depends_on:
      - prometheus
      - loki
      - jaeger
    networks:
      - bank-net

volumes:
  prometheus-data:
  loki-data:
  grafana-data:

networks:
  bank-net:
    external: true
```

### 4.2 Atualizar `docker-compose.yml`

**Opção A**: Integrar tudo em um único arquivo
**Opção B**: Usar extends e incluir observability

```bash
# Opção B - Usar múltiplos arquivos
podman-compose -f docker-compose.yml -f docker-compose.observability.yml up -d
```

## ✅ Fase 5: Validação e Testes (45 min)

### 5.1 Verificar Serviços

```bash
# Status de todos os containers
podman ps

# Verificar logs
podman logs otel-collector
podman logs jaeger
podman logs prometheus
podman logs loki
podman logs grafana

# Health checks
curl http://localhost:13133  # OTel Collector health
curl http://localhost:16686  # Jaeger UI
curl http://localhost:9090   # Prometheus UI
curl http://localhost:3100/ready  # Loki readiness
curl http://localhost:3000   # Grafana UI
```

### 5.2 Testar Fluxo de Dados

#### Traces
1. Acessar Jaeger: `http://localhost:16686`
2. Selecionar serviço (ex: `api`)
3. Buscar traces recentes
4. Verificar spans e latências

#### Métricas
1. Acessar Prometheus: `http://localhost:9090`
2. Executar query: `rate(http_server_request_duration_count[5m])`
3. Verificar se há dados dos serviços

#### Logs
1. Acessar Grafana: `http://localhost:3000`
2. Ir para Explore
3. Selecionar Loki datasource
4. Query: `{service_name="api"}`
5. Verificar logs estruturados

### 5.3 Correlação de Dados

**Teste de Correlação**:
1. Fazer requisição à API: `curl http://localhost:5001/api/accounts/10000001`
2. Copiar `TraceId` do response header ou log
3. Buscar no Jaeger pelo TraceId
4. No Grafana/Loki, filtrar logs: `{service_name="api"} |= "TraceId={copiado}"`

## 📈 Fase 6: Dashboards e Alertas (1h)

### 6.1 Dashboards Essenciais

**Dashboard 1: Overview Geral**
- Taxa de requisições (QPS)
- Latência P50, P95, P99
- Taxa de erro
- CPU e memória por serviço

**Dashboard 2: Por Serviço**
- Latência por endpoint
- Distribuição de status codes
- Taxa de erro por endpoint
- Dependencies

**Dashboard 3: Logs e Traces Correlacionados**
- Logs recentes por nível
- Traces com alta latência
- Erros agrupados
- Link direto para Jaeger

### 6.2 Alertas Importantes

```yaml
# Exemplo de regra de alerta Prometheus
groups:
  - name: mynewlittlebank
    rules:
      - alert: HighErrorRate
        expr: rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "Alta taxa de erros no serviço {{ $labels.service_name }}"
          
      - alert: HighLatency
        expr: histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m])) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Alta latência P95 no serviço {{ $labels.service_name }}"
```

## 🔍 Fase 7: Otimizações (30 min)

### 7.1 Sampling de Traces

Para ambientes com alto volume, configurar sampling:

```yaml
# otel-collector.yaml
processors:
  probabilistic_sampler:
    sampling_percentage: 10  # Amostra 10% dos traces
```

### 7.2 Retenção de Dados

Ajustar conforme necessidade de armazenamento:

```yaml
# Prometheus
--storage.tsdb.retention.time=30d

# Loki
limits_config:
  retention_period: 30d
```

### 7.3 Performance do Collector

```yaml
# otel-collector.yaml
processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
    send_batch_max_size: 2048
  
  memory_limiter:
    check_interval: 1s
    limit_mib: 512
    spike_limit_mib: 128
```

## 📚 Fase 8: Documentação (30 min)

### 8.1 Atualizar README

Adicionar seção de observabilidade:
- URLs de acesso
- Credenciais padrão
- Exemplos de queries
- Troubleshooting comum

### 8.2 Criar Guias

1. **OBSERVABILITY.md**: Este documento
2. **TROUBLESHOOTING.md**: Problemas comuns e soluções
3. **QUERIES.md**: Queries úteis para Prometheus e Loki
4. **DASHBOARDS.md**: Guia dos dashboards disponíveis

## ⏱️ Cronograma Total: ~6 horas

| Fase | Duração | Descrição |
|------|---------|-----------|
| 1 | 30 min | Preparação e backup |
| 2 | 1h | Configuração dos componentes |
| 3 | 1h 30min | Atualização da aplicação |
| 4 | 30 min | Docker Compose |
| 5 | 45 min | Validação e testes |
| 6 | 1h | Dashboards e alertas |
| 7 | 30 min | Otimizações |
| 8 | 30 min | Documentação |
| **Total** | **~6h** | |

## 🎯 Entregáveis

- [ ] Stack de observabilidade funcionando
- [ ] Traces visíveis no Jaeger
- [ ] Métricas no Prometheus
- [ ] Logs no Loki
- [ ] Grafana com datasources configurados
- [ ] Dashboards básicos criados
- [ ] Documentação atualizada
- [ ] Guia de troubleshooting

## 🔗 Referências

- [Repositório Base: robersonnaves/Telemetry](https://github.com/robersonnaves/Telemetry)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Loki Documentation](https://grafana.com/docs/loki/)
- [Grafana Documentation](https://grafana.com/docs/grafana/)

## 💡 Próximos Passos (Melhorias Futuras)

1. **Tempo**: Adicionar Grafana Tempo para traces persistentes
2. **AlertManager**: Configurar notificações de alertas
3. **Storage Persistente**: Migrar Jaeger de memória para Elasticsearch/Cassandra
4. **Service Mesh**: Considerar Istio/Linkerd para observabilidade adicional
5. **Distributed Tracing**: Implementar baggage propagation
6. **Custom Metrics**: Criar métricas de negócio específicas
7. **Log Aggregation**: Adicionar Fluentd/Fluent Bit para logs adicionais
8. **APM**: Considerar adicionar APM solution (Elastic APM, Datadog, etc)

---

**Autor**: Plano gerado automaticamente  
**Data**: 2026-01-04  
**Versão**: 1.0

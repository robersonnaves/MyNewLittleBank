# Observability Architecture - MyNewLittleBank

## 📊 Overview

O projeto MyNewLittleBank utiliza uma stack de observabilidade completa com **OpenTelemetry Collector** como hub central, roteando telemetria para múltiplos backends:

- **Aspire Dashboard** - Visualização em tempo real (desenvolvimento)
- **Jaeger** - Rastreamento distribuído de traces
- **Prometheus** - Armazenamento e consulta de métricas time-series
- **Loki** - Centralização de logs estruturados
- **Grafana** - Visualização unificada de todos os sinais

## 🏗️ Architecture

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
│ (Traces)│ │(Metrics)│ │ (Logs)  │ HTTP Push API
└────┬────┘ └────┬────┘ └────┬────┘
     │           │           │
     └───────────┴───────────┴──────────┐
                                        │
                            ┌───────────▼─────────┐
                            │      Grafana        │
                            │   (Visualização)    │
                            └─────────────────────┘
                                    │
                                    │ (também)
                                    ▼
                            ┌─────────────────────┐
                            │  Aspire Dashboard   │
                            │   (Desenvolvimento) │
                            └─────────────────────┘
```

## 📦 Componentes da Stack

| Componente | Função | Porta Principal | URL |
|------------|--------|-----------------|-----|
| **OpenTelemetry Collector** | Coletor central de telemetria | 4317 (gRPC), 4318 (HTTP) | - |
| **Jaeger** | Backend de traces distribuídos | 16686 (UI) | http://localhost:16686 |
| **Prometheus** | Backend de métricas time-series | 9090 (UI) | http://localhost:9090 |
| **Loki** | Backend de logs estruturados | 3100 (API) | http://localhost:3100 |
| **Grafana** | Visualização unificada | 3000 (UI) | http://localhost:3000 |
| **Aspire Dashboard** | Visualização em tempo real (dev) | 15000 (UI) | http://localhost:15000 |

## 🚀 Iniciando a Stack de Observabilidade

### Opção 1: Stack Completa (Recomendado)

```bash
cd infra

# Iniciar stack principal (aplicações)
podman-compose up -d

# Iniciar stack de observabilidade
podman-compose -f docker-compose.observability.yml up -d
```

### Opção 2: Apenas Aspire (Desenvolvimento Rápido)

```bash
cd infra
podman-compose up -d
# Aspire Dashboard estará disponível em http://localhost:15000
```

### Parar a Stack de Observabilidade

```bash
cd infra
podman-compose -f docker-compose.observability.yml down
```

## 🔧 Configuração

### Variáveis de Ambiente dos Serviços .NET

Todos os serviços .NET utilizam as mesmas variáveis de ambiente:

```yaml
environment:
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
  - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  - OTEL_SERVICE_NAME=<service-name>
  - OTEL_RESOURCE_ATTRIBUTES=deployment.environment=local,service.version=1.0.0
  - OTEL_LOGS_EXPORTER=otlp
  - OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE=true
```

### OpenTelemetry Collector

**Arquivo**: `infra/config/otel-collector.yaml`

**Receivers**:
- OTLP gRPC na porta 4317
- OTLP HTTP na porta 4318

**Processors**:
- `resourcedetection` - Detecta ambiente e hostname automaticamente
- `resource` - Enriquece com service.name e deployment.environment
- `batch` - Agrupa dados para otimizar envio
- `attributes` - Adiciona/modifica atributos
- `memory_limiter` - Limita uso de memória (512MB)

**Exporters**:
- `otlp/aspire` - Aspire Dashboard (compatibilidade)
- `otlp/jaeger` - Jaeger para traces
- `prometheus` - Exposição de métricas em /metrics
- `loki` - Loki para logs

**Pipelines**:
- `traces` → Jaeger + Aspire
- `metrics` → Prometheus + Aspire
- `logs` → Loki + Aspire

### Jaeger

**Configuração**: Storage em memória (desenvolvimento)

**Recursos**:
- Busca de traces por serviço, operação, tags
- Visualização de spans hierárquicos
- Mapa de dependências entre serviços
- Análise de latências

**Acesso**: http://localhost:16686

### Prometheus

**Arquivo**: `infra/config/prometheus.yml`

**Configuração**:
- Scrape interval: 15s
- Retention: 7 dias
- Targets:
  - `otel-collector:8889` - Métricas do collector
  - `prometheus:9090` - Auto-monitoramento

**Acesso**: http://localhost:9090

**Queries Úteis**:
```promql
# Taxa de requisições
rate(http_server_request_duration_count[5m])

# Latência P95
histogram_quantile(0.95, rate(http_server_request_duration_bucket[5m]))

# Erros por endpoint
sum by (http_route) (rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]))
```

### Loki

**Arquivo**: `infra/config/loki.yml`

**Configuração**:
- Schema: v13 com TSDB
- Storage: Filesystem persistente
- Retention: 7 dias
- Suporte a structured metadata e pattern ingestion

**Labels Automáticos**:
- `service_name`: Nome do serviço
- `level`: Nível do log (INFO, ERROR, etc)
- `deployment_environment`: Ambiente (local, prod, etc)

**Acesso via Grafana**: http://localhost:3000 → Explore → Loki

**Queries Úteis**:
```logql
# Logs de um serviço específico
{service_name="api"}

# Logs de erro
{service_name="api"} |= "ERROR"

# Logs com TraceId específico
{service_name="api"} |= "TraceId=abc123"
```

### Grafana

**Configuração**: Auto-provisioning de datasources e dashboards

**Datasources**:
- Prometheus (http://prometheus:9090)
- Loki (http://loki:3100)
- Jaeger (http://jaeger:16686)

**Credenciais Padrão**:
- Usuário: `admin`
- Senha: `admin`

**Acesso**: http://localhost:3000

## ✅ Validação

### Verificar Serviços

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

### Testar Fluxo de Dados

#### Traces
1. Acessar Jaeger: http://localhost:16686
2. Selecionar serviço (ex: `api`)
3. Buscar traces recentes
4. Verificar spans e latências

#### Métricas
1. Acessar Prometheus: http://localhost:9090
2. Executar query: `rate(http_server_request_duration_count[5m])`
3. Verificar se há dados dos serviços

#### Logs
1. Acessar Grafana: http://localhost:3000
2. Ir para Explore
3. Selecionar Loki datasource
4. Query: `{service_name="api"}`
5. Verificar logs estruturados

### Correlação de Dados

**Teste de Correlação**:
1. Fazer requisição à API: `curl http://localhost:5001/api/accounts/10000001`
2. Copiar `TraceId` do response header ou log
3. Buscar no Jaeger pelo TraceId
4. No Grafana/Loki, filtrar logs: `{service_name="api"} |= "TraceId={copiado}"`

## 🐛 Troubleshooting

### Telemetry Não Aparece nos Backends

1. **Verificar OTel Collector**:
   ```bash
   podman logs otel-collector --tail 100
   ```
   Procurar por erros de conexão ou export failures

2. **Verificar se backends estão prontos**:
   ```bash
   podman ps --format "table {{.Names}}\t{{.Status}}"
   ```
   Todos devem estar `Up` ou `Up (healthy)`

3. **Verificar variáveis de ambiente dos serviços**:
   ```bash
   podman exec api printenv | grep OTEL
   ```

### Jaeger Não Mostra Traces

1. Verificar se Jaeger está recebendo dados:
   ```bash
   podman logs jaeger | grep "OTLP"
   ```

2. Verificar configuração do Collector:
   ```bash
   podman exec otel-collector cat /etc/otel-collector-config.yaml | grep jaeger
   ```

### Prometheus Não Coleta Métricas

1. Verificar targets no Prometheus UI: http://localhost:9090/targets
2. Verificar se Collector expõe métricas:
   ```bash
   curl http://localhost:8889/metrics
   ```

### Loki Não Recebe Logs

1. Verificar se Loki está pronto:
   ```bash
   curl http://localhost:3100/ready
   ```

2. Verificar configuração do Collector para Loki:
   ```bash
   podman logs otel-collector | grep -i loki
   ```

### Grafana Não Conecta aos Datasources

1. Verificar logs do Grafana:
   ```bash
   podman logs grafana | grep -i datasource
   ```

2. Verificar arquivo de configuração:
   ```bash
   cat infra/config/grafana/datasources/datasources.yml
   ```

## 📈 Performance e Recursos

### Uso de Recursos (Estimado)

- **OTel Collector**: ~50-100MB RAM
- **Jaeger**: ~200-300MB RAM (memória)
- **Prometheus**: ~100-200MB RAM
- **Loki**: ~100-150MB RAM
- **Grafana**: ~100-150MB RAM
- **Total**: ~550-900MB RAM adicional

### Retenção de Dados

- **Jaeger**: Memória (dados perdidos ao reiniciar)
- **Prometheus**: 7 dias (configurável)
- **Loki**: 7 dias (configurável)
- **Grafana**: Dashboards e configurações persistentes

## 🔒 Segurança

### Configuração Atual (Desenvolvimento)

- Sem autenticação nos backends
- Sem TLS entre componentes
- Portas expostas para host

### Recomendações para Produção

- Habilitar autenticação no Grafana
- Configurar TLS para OTLP
- Restringir exposição de portas
- Usar storage persistente para Jaeger
- Configurar AlertManager para notificações
- Considerar serviços gerenciados (Grafana Cloud, etc)

## 📚 Referências

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Loki Documentation](https://grafana.com/docs/loki/)
- [Grafana Documentation](https://grafana.com/docs/grafana/)
- [Repositório Base: robersonnaves/Telemetry](https://github.com/robersonnaves/Telemetry)

---

**Última Atualização**: 2026-01-04  
**Status**: ✅ Stack Completa Implementada  
**Arquivos de Configuração**: `infra/config/`

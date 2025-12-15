# Changelog - Implementação OpenTelemetry Collector

## 2025-12-15

### ✅ Correções Implementadas

#### 1. **Correção do PrometheusExporter**
**Problema:** Aplicações falhavam ao iniciar com erro:
```
System.ArgumentException: A PrometheusExporter could not be found configured on the provided MeterProvider.
```

**Causa:** O `PrometheusExporter` não estava sendo adicionado ao `MeterProvider` no método `AddObservability()`.

**Solução:** Adicionado `metrics.AddPrometheusExporter();` em `src/Shared/Observability/ObservabilityExtensions.cs` (linha 97).

**Arquivo modificado:** `src/Shared/Observability/ObservabilityExtensions.cs`

---

#### 2. **Implementação do OpenTelemetry Collector**
**Motivo:** Centralizar coleta de telemetria, melhorar performance, e facilitar roteamento para múltiplos backends.

**Componentes adicionados:**

**a) OpenTelemetry Collector Container**
- **Imagem:** `otel/opentelemetry-collector-contrib:0.112.0`
- **Configuração:** `infra/otel-collector-config.yaml`
- **Portas expostas:**
  - `4317`: OTLP gRPC receiver (usado pelas aplicações)
  - `4318`: OTLP HTTP receiver
  - `8889`: Prometheus metrics exporter
  - `8888`: Collector internal metrics
  - `13133`: Health check endpoint
  - `55679`: zPages (debug)

**b) Pipelines configurados:**
- **Traces:** OTLP → [memory_limiter, batch, attributes] → Jaeger + Debug
- **Metrics:** OTLP → [memory_limiter, batch, attributes] → Prometheus + Debug
- **Logs:** OTLP → [memory_limiter, batch, attributes] → Debug

**c) Correção do Exporter Depreciado**
**Problema:** Collector falhava ao iniciar com erro:
```
error decoding 'exporters': the logging exporter has been deprecated, use the debug exporter instead
```

**Solução:** Substituído `logging` exporter por `debug` exporter no arquivo de configuração.

**Arquivo modificado:** `infra/otel-collector-config.yaml`

---

### 📁 Arquivos Criados

1. **`infra/otel-collector-config.yaml`**
   - Configuração completa do OpenTelemetry Collector
   - Receivers: OTLP (gRPC + HTTP)
   - Processors: batch, memory_limiter, attributes
   - Exporters: Jaeger (OTLP), Prometheus, Debug
   - Extensions: health_check, pprof, zpages

2. **`infra/OBSERVABILITY.md`**
   - Documentação completa da stack de observabilidade
   - Diagramas de arquitetura
   - Guia de uso (Jaeger, Prometheus, OTel Collector)
   - Consultas úteis (PromQL)
   - Troubleshooting

3. **`infra/CHANGELOG-OBSERVABILITY.md`** (este arquivo)
   - Registro de mudanças e correções

---

### 📝 Arquivos Modificados

#### 1. `src/Shared/Observability/ObservabilityExtensions.cs`
**Linha 97:** Adicionado `metrics.AddPrometheusExporter();`

#### 2. `infra/docker-compose.yml`
**Alterações:**
- Adicionado serviço `otel-collector` (linhas 74-94)
- Modificado serviço `jaeger`: removidas portas 4317/4318 (agora gerenciadas pelo collector)
- Adicionada dependência `otel-collector` em todos os serviços .NET:
  - `api`
  - `mock-transactions`
  - `services-card`
  - `services-heartbeats`
  - `services-money`
  - `services-pix`

#### 3. `infra/prometheus.yml`
**Adicionado:**
- Job `otel-collector`: Scrape de métricas internas do collector (porta 8888)
- Job `mynewlittlebank-services`: Scrape de métricas das aplicações via collector (porta 8889)

---

### ✅ Status Atual

**Todos os serviços funcionando corretamente:**

```bash
$ podman-compose ps
CONTAINER ID  IMAGE                                                   STATUS
97e54f919073  postgres:16-alpine                                      Up (healthy)
48670f6b59b7  rabbitmq:3.13-management                                Up (healthy)
78a367cb2e8b  jaegertracing/all-in-one:1.58                           Up (healthy)
b1e6cb856085  opensearchproject/opensearch:2.15.0                     Up (healthy)
09791cecd89d  prom/prometheus:v2.54.1                                 Up (healthy)
9b8b4527313e  otel/opentelemetry-collector-contrib:0.112.0            Up (healthy)
ab8868e569d1  dpage/pgadmin4                                          Up
16ad3ba005d1  mynewlittlebank_mock-transactions                       Up
dab0fbbebe0f  mynewlittlebank_api                                     Up
44b4555a0ef3  mynewlittlebank_services-card                           Up
a90c38fd7729  mynewlittlebank_services-heartbeats                     Up
94843a346ee2  mynewlittlebank_services-money                          Up
8dff5292ee8d  mynewlittlebank_services-pix                            Up
```

**Endpoints disponíveis:**
- **Jaeger UI:** http://localhost:16686
- **Prometheus UI:** http://localhost:9090
- **OTel Collector Health:** http://localhost:13133
- **OTel Collector Metrics:** http://localhost:8889/metrics
- **OTel Collector zPages:** http://localhost:55679
- **API Swagger:** http://localhost:5000/swagger

---

### 🎯 Próximos Passos (Recomendações)

1. **Monitoramento:**
   - Configurar alertas no Prometheus para métricas críticas
   - Criar dashboards no Grafana (opcional)

2. **Performance:**
   - Ajustar batch size e timeout no collector conforme volume de dados
   - Monitorar uso de memória do collector

3. **Segurança:**
   - Configurar autenticação/TLS para endpoints públicos (produção)
   - Restringir acesso aos endpoints de debug (zPages, pprof)

4. **Logging:**
   - Considerar adicionar exporter para logs (ex: Loki, OpenSearch)
   - Atualmente logs vão apenas para Debug exporter (console)

---

### 📚 Referências

- [OpenTelemetry Collector Documentation](https://opentelemetry.io/docs/collector/)
- [OTel Collector Configuration](https://opentelemetry.io/docs/collector/configuration/)
- [Debug Exporter (substitui Logging)](https://github.com/open-telemetry/opentelemetry-collector/tree/main/exporter/debugexporter)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)

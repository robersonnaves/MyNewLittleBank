## 1. Preparação e Estrutura

- [x] 1.1 Criar backup das configurações atuais (docker-compose.yml, otel-collector-config.yaml)
- [x] 1.2 Criar estrutura de diretórios: `infra/config/grafana/{datasources,dashboards}`
- [x] 1.3 Criar diretórios de dados: `infra/data/{prometheus,loki,grafana,jaeger}` (se necessário para volumes)

## 2. Configuração do OpenTelemetry Collector

- [x] 2.1 Atualizar `infra/config/otel-collector.yaml` com receivers OTLP (gRPC 4317, HTTP 4318)
- [x] 2.2 Adicionar processors: resourcedetection, resource, batch, attributes, memory_limiter
- [x] 2.3 Configurar exporter OTLP para Jaeger (traces via gRPC)
- [x] 2.4 Configurar exporter Prometheus (métricas em /metrics endpoint)
- [x] 2.5 Configurar exporter Loki (logs via HTTP Push API)
- [x] 2.6 Configurar pipelines: traces → Jaeger, metrics → Prometheus, logs → Loki
- [x] 2.7 Manter exportação para Aspire Dashboard (compatibilidade)

## 3. Configuração do Jaeger

- [x] 3.1 Criar configuração do Jaeger no docker-compose.observability.yml
- [x] 3.2 Configurar `jaegertracing/all-in-one:latest` com OTLP habilitado
- [x] 3.3 Expor porta 16686 para UI
- [x] 3.4 Configurar storage em memória (desenvolvimento)

## 4. Configuração do Prometheus

- [x] 4.1 Criar `infra/config/prometheus.yml` com configuração de scrape
- [x] 4.2 Configurar scrape interval de 15s
- [x] 4.3 Configurar retention de 7 dias
- [x] 4.4 Adicionar targets: otel-collector:8889, prometheus:9090
- [x] 4.5 Criar volume para dados persistentes

## 5. Configuração do Loki

- [x] 5.1 Criar `infra/config/loki.yml` com schema v13 e TSDB
- [x] 5.2 Configurar storage filesystem persistente
- [x] 5.3 Configurar retention de 7 dias
- [x] 5.4 Habilitar suporte a structured metadata e pattern ingestion
- [x] 5.5 Configurar rate limiting para desenvolvimento

## 6. Configuração do Grafana

- [x] 6.1 Criar `infra/config/grafana/datasources/datasources.yml`
- [x] 6.2 Configurar datasource Prometheus (http://prometheus:9090)
- [x] 6.3 Configurar datasource Loki (http://loki:3100)
- [x] 6.4 Configurar datasource Jaeger (http://jaeger:16686)
- [x] 6.5 Criar `infra/config/grafana/dashboards/dashboards.yml` para auto-provisioning
- [x] 6.6 Configurar credenciais padrão (admin/admin) via variáveis de ambiente

## 7. Docker Compose

- [x] 7.1 Criar `infra/docker-compose.observability.yml` com todos os serviços
- [x] 7.2 Configurar network `bank-net` como external
- [x] 7.3 Configurar volumes para dados persistentes (prometheus, loki, grafana)
- [x] 7.4 Configurar health checks e depends_on apropriados
- [x] 7.5 Atualizar `infra/docker-compose.yml` se necessário para garantir compatibilidade

## 8. Atualização dos Serviços .NET

- [x] 8.1 Verificar que variáveis de ambiente OTEL_EXPORTER_OTLP_ENDPOINT apontam para otel-collector:4317
- [x] 8.2 Verificar que OTEL_EXPORTER_OTLP_PROTOCOL=grpc está configurado
- [x] 8.3 Verificar que OTEL_SERVICE_NAME está definido para cada serviço
- [x] 8.4 Verificar que OTEL_RESOURCE_ATTRIBUTES inclui deployment.environment
- [x] 8.5 Verificar que OTEL_LOGS_EXPORTER=otlp está configurado
- [x] 8.6 Garantir que logs usam structured logging (ILogger com placeholders)

## 9. Validação e Testes

- [x] 9.1 Iniciar stack de observabilidade: `podman-compose -f docker-compose.observability.yml up -d`
- [x] 9.2 Verificar health checks de todos os serviços
- [x] 9.3 Testar recepção de traces no Jaeger (acessar UI em http://localhost:16686)
- [x] 9.4 Testar recepção de métricas no Prometheus (acessar UI em http://localhost:9090)
- [x] 9.5 Testar recepção de logs no Loki via Grafana (acessar http://localhost:3000)
- [x] 9.6 Validar correlação: fazer requisição à API, buscar TraceId no Jaeger, filtrar logs no Loki
- [x] 9.7 Verificar que Aspire Dashboard continua recebendo dados (compatibilidade)

## 10. Dashboards e Visualização

- [x] 10.1 Criar dashboard "Overview Geral" no Grafana com QPS, latência P50/P95/P99, taxa de erro
- [x] 10.2 Criar dashboard "Por Serviço" com latência por endpoint, status codes, dependências
- [x] 10.3 Criar dashboard "Logs e Traces Correlacionados" com logs recentes, traces de alta latência
- [x] 10.4 Configurar auto-provisioning de dashboards via arquivos JSON (opcional)

## 11. Documentação

- [x] 11.1 Atualizar `infra/OBSERVABILITY.md` com arquitetura completa
- [x] 11.2 Documentar URLs de acesso (Jaeger, Prometheus, Loki, Grafana)
- [x] 11.3 Documentar credenciais padrão
- [x] 11.4 Criar guia de queries úteis para Prometheus e Loki
- [x] 11.5 Criar guia de troubleshooting comum
- [x] 11.6 Documentar como iniciar/parar stack de observabilidade

## 12. Otimizações (Opcional para Fase Inicial)

- [x] 12.1 Configurar sampling de traces (probabilistic_sampler) se necessário
- [x] 12.2 Ajustar retenção de dados conforme necessidade
- [x] 12.3 Otimizar configuração de batch processor no Collector
- [x] 12.4 Configurar memory_limiter apropriado para ambiente

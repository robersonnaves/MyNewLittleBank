## 1. Preparação da Estrutura

- [x] 1.1 Criar diretório `infra/config/` para arquivos de configuração
- [x] 1.2 Criar subdiretórios: `grafana/datasources`, `grafana/dashboards`, `grafana/provisioning`
- [x] 1.3 Criar diretórios de dados: `data/prometheus`, `data/loki`, `data/tempo`, `data/grafana`, `data/pyroscope` (ou usar volumes Docker)

## 2. Configuração do OpenTelemetry Collector

- [x] 2.1 Ler configuração atual do collector (se existir em `infra/config/otel-collector.yaml`)
- [x] 2.2 Adicionar exporter `otlp/tempo` para traces (endpoint: `tempo:4317`)
- [x] 2.3 Adicionar exporter `otlphttp/logs` para logs (endpoint: `http://loki/otlp` - sem porta, Collector adiciona `/v1/logs` automaticamente)
- [x] 2.4 Adicionar exporter `prometheus` para métricas (endpoint: `0.0.0.0:8889`)
- [x] 2.5 Adicionar exporter `otlphttp/pyroscope` para profiling (endpoint: `http://pyroscope:4040`)
- [x] 2.6 Atualizar pipelines: `traces` → Tempo, `logs` → Loki, `metrics` → Prometheus
- [x] 2.7 Validar sintaxe YAML do arquivo de configuração

## 3. Configuração do Prometheus

- [x] 3.1 Criar arquivo `infra/config/prometheus.yml`
- [x] 3.2 Configurar scrape de `otel-collector:8889/metrics` (métricas do collector, scrape_interval: 15s)
- [x] 3.3 Configurar retenção de 7 dias
- [x] 3.4 Configurar storage path: `/prometheus`
- [x] 3.5 Adicionar configuração de alertas (vazio por enquanto)

## 4. Configuração do Loki

- [x] 4.1 Criar arquivo `infra/config/loki.yml`
- [x] 4.2 Configurar schema v13 com TSDB
- [x] 4.3 Configurar storage filesystem: `/loki`
- [x] 4.4 Configurar retenção de 7 dias
- [x] 4.5 Configurar limites de rate limiting para desenvolvimento
- [x] 4.6 Habilitar structured metadata e pattern ingestion

## 5. Configuração do Tempo

- [x] 5.1 Criar arquivo `infra/config/tempo.yml`
- [x] 5.2 Configurar receiver OTLP gRPC na porta 4317 (OTLP HTTP na 4318 opcional)
- [x] 5.3 Configurar storage local: `/var/tempo`
- [x] 5.4 Configurar retenção de 7 dias
- [x] 5.5 Configurar compactor para otimização de storage

## 6. Configuração do Pyroscope

- [x] 6.1 Criar arquivo `infra/config/pyroscope.yml`
- [x] 6.2 Configurar storage local: `/var/lib/pyroscope`
- [x] 6.3 Configurar retenção de 7 dias
- [x] 6.4 Configurar API server na porta 4040
- [x] 6.5 Habilitar suporte a múltiplos formatos (pprof, JFR, etc)

## 7. Configuração do Grafana

- [x] 7.1 Criar arquivo `infra/config/grafana/datasources/datasources.yml`
- [x] 7.2 Configurar datasource Prometheus (URL: `http://prometheus:9090`, `httpMethod: POST`, exemplarTraceIdDestinations para Tempo)
- [x] 7.3 Configurar datasource Loki (URL: `http://loki:3100`, `timeout: 60`, `maxLines: 1000`)
- [x] 7.4 Configurar datasource Tempo (URL: `http://tempo:3200`, com `tracesToLogs`, `tracesToMetrics`, `tracesToProfiles` para correlação)
- [x] 7.5 Configurar datasource Pyroscope (URL: `http://pyroscope:4040`)
- [x] 7.6 Criar arquivo `infra/config/grafana/provisioning/dashboards/dashboards.yml` (vazio, para futuros dashboards)

## 8. Docker Compose

- [x] 8.1 Criar arquivo `infra/docker-compose.observability.yml`
- [x] 8.2 Adicionar serviço `prometheus` com configuração, volumes e rede
- [x] 8.3 Adicionar serviço `loki` com configuração, volumes e rede
- [x] 8.4 Adicionar serviço `tempo` com configuração, volumes e rede
- [x] 8.5 Adicionar serviço `pyroscope` com configuração, volumes e rede
- [x] 8.6 Adicionar serviço `grafana` com provisioning, volumes, dependências e rede
- [x] 8.7 Configurar volumes persistentes para todos os serviços
- [x] 8.8 Configurar rede `bank-net` (mesma da stack principal)
- [x] 8.9 Adicionar health checks onde aplicável
- [x] 8.10 Configurar variáveis de ambiente para credenciais (admin/admin para dev)

## 9. Atualização do Collector Principal

- [x] 9.1 Verificar se `infra/config/otel-collector.yaml` existe
- [x] 9.2 Se não existir, criar a partir da configuração atual no docker-compose
- [x] 9.3 Atualizar volume mount no `infra/docker-compose.yml` para usar arquivo de config
- [x] 9.4 Validar que collector continua funcionando após mudanças

## 10. Documentação

- [x] 10.1 Atualizar `infra/OBSERVABILITY.md` com informações da nova stack
- [x] 10.2 Documentar URLs de acesso (Grafana: 3000, Prometheus: 9090, etc)
- [x] 10.3 Documentar credenciais padrão (admin/admin)
- [x] 10.4 Adicionar seção de troubleshooting
- [x] 10.5 Adicionar exemplos de queries úteis para Prometheus, Loki e Tempo

## 11. Validação

**Nota**: Estas tarefas devem ser executadas após iniciar a stack de observabilidade.

- [ ] 11.1 Iniciar stack de observabilidade: `podman-compose -f docker-compose.observability.yml up -d`
- [ ] 11.2 Verificar logs de todos os serviços para erros
- [ ] 11.3 Validar health checks: Prometheus `/targets`, Loki `/ready`, Tempo `/ready`, Grafana `/api/health`
- [ ] 11.4 Verificar recebimento de traces no Tempo (via Grafana Explore)
- [ ] 11.5 Verificar recebimento de métricas no Prometheus (query: `up`)
- [ ] 11.6 Verificar recebimento de logs no Loki (query: `{service_name="api"}`)
- [ ] 11.7 Validar datasources configurados no Grafana
- [ ] 11.8 Testar correlação: buscar trace no Tempo, correlacionar com logs no Loki usando traceId

## 12. Testes de Integração

**Nota**: Estas tarefas devem ser executadas após iniciar a stack de observabilidade e a stack principal.

- [ ] 12.1 Fazer requisição à API e verificar trace completo no Tempo
- [ ] 12.2 Verificar métricas de HTTP aparecendo no Prometheus
- [ ] 12.3 Verificar logs estruturados aparecendo no Loki
- [ ] 12.4 Validar que profiling pode ser enviado ao Pyroscope (se aplicável)
- [ ] 12.5 Testar reinicialização da stack (dados devem persistir nos volumes)


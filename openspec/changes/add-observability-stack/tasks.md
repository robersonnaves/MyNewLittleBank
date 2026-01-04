## 1. Preparação da Estrutura

- [ ] 1.1 Criar diretório `infra/config/` para arquivos de configuração
- [ ] 1.2 Criar subdiretórios: `grafana/datasources`, `grafana/dashboards`, `grafana/provisioning`
- [ ] 1.3 Criar diretórios de dados: `data/prometheus`, `data/loki`, `data/tempo`, `data/grafana`, `data/pyroscope` (ou usar volumes Docker)

## 2. Configuração do OpenTelemetry Collector

- [ ] 2.1 Ler configuração atual do collector (se existir em `infra/config/otel-collector.yaml`)
- [ ] 2.2 Adicionar exporter `otlp/tempo` para traces (endpoint: `tempo:4317`)
- [ ] 2.3 Adicionar exporter `loki` para logs (endpoint: `http://loki:3100/otlp`)
- [ ] 2.4 Adicionar exporter `prometheus` para métricas (endpoint: `0.0.0.0:8889`)
- [ ] 2.5 Adicionar exporter `otlphttp/pyroscope` para profiling (endpoint: `http://pyroscope:4040`)
- [ ] 2.6 Atualizar pipelines: `traces` → Tempo, `logs` → Loki, `metrics` → Prometheus
- [ ] 2.7 Validar sintaxe YAML do arquivo de configuração

## 3. Configuração do Prometheus

- [ ] 3.1 Criar arquivo `infra/config/prometheus.yml`
- [ ] 3.2 Configurar scrape de `otel-collector:8889` (métricas do collector)
- [ ] 3.3 Configurar retenção de 7 dias
- [ ] 3.4 Configurar storage path: `/prometheus`
- [ ] 3.5 Adicionar configuração de alertas (vazio por enquanto)

## 4. Configuração do Loki

- [ ] 4.1 Criar arquivo `infra/config/loki.yml`
- [ ] 4.2 Configurar schema v13 com TSDB
- [ ] 4.3 Configurar storage filesystem: `/loki`
- [ ] 4.4 Configurar retenção de 7 dias
- [ ] 4.5 Configurar limites de rate limiting para desenvolvimento
- [ ] 4.6 Habilitar structured metadata e pattern ingestion

## 5. Configuração do Tempo

- [ ] 5.1 Criar arquivo `infra/config/tempo.yml`
- [ ] 5.2 Configurar receiver OTLP na porta 4317
- [ ] 5.3 Configurar storage local: `/var/tempo`
- [ ] 5.4 Configurar retenção de 7 dias
- [ ] 5.5 Configurar compactor para otimização de storage

## 6. Configuração do Pyroscope

- [ ] 6.1 Criar arquivo `infra/config/pyroscope.yml`
- [ ] 6.2 Configurar storage local: `/var/lib/pyroscope`
- [ ] 6.3 Configurar retenção de 7 dias
- [ ] 6.4 Configurar API server na porta 4040
- [ ] 6.5 Habilitar suporte a múltiplos formatos (pprof, JFR, etc)

## 7. Configuração do Grafana

- [ ] 7.1 Criar arquivo `infra/config/grafana/datasources/datasources.yml`
- [ ] 7.2 Configurar datasource Prometheus (URL: `http://prometheus:9090`)
- [ ] 7.3 Configurar datasource Loki (URL: `http://loki:3100`)
- [ ] 7.4 Configurar datasource Tempo (URL: `http://tempo:3200`)
- [ ] 7.5 Configurar datasource Pyroscope (URL: `http://pyroscope:4040`)
- [ ] 7.6 Criar arquivo `infra/config/grafana/provisioning/dashboards/dashboards.yml` (vazio, para futuros dashboards)

## 8. Docker Compose

- [ ] 8.1 Criar arquivo `infra/docker-compose.observability.yml`
- [ ] 8.2 Adicionar serviço `prometheus` com configuração, volumes e rede
- [ ] 8.3 Adicionar serviço `loki` com configuração, volumes e rede
- [ ] 8.4 Adicionar serviço `tempo` com configuração, volumes e rede
- [ ] 8.5 Adicionar serviço `pyroscope` com configuração, volumes e rede
- [ ] 8.6 Adicionar serviço `grafana` com provisioning, volumes, dependências e rede
- [ ] 8.7 Configurar volumes persistentes para todos os serviços
- [ ] 8.8 Configurar rede `bank-net` (mesma da stack principal)
- [ ] 8.9 Adicionar health checks onde aplicável
- [ ] 8.10 Configurar variáveis de ambiente para credenciais (admin/admin para dev)

## 9. Atualização do Collector Principal

- [ ] 9.1 Verificar se `infra/config/otel-collector.yaml` existe
- [ ] 9.2 Se não existir, criar a partir da configuração atual no docker-compose
- [ ] 9.3 Atualizar volume mount no `infra/docker-compose.yml` para usar arquivo de config
- [ ] 9.4 Validar que collector continua funcionando após mudanças

## 10. Documentação

- [ ] 10.1 Atualizar `infra/OBSERVABILITY.md` com informações da nova stack
- [ ] 10.2 Documentar URLs de acesso (Grafana: 3000, Prometheus: 9090, etc)
- [ ] 10.3 Documentar credenciais padrão (admin/admin)
- [ ] 10.4 Adicionar seção de troubleshooting
- [ ] 10.5 Adicionar exemplos de queries úteis para Prometheus, Loki e Tempo

## 11. Validação

- [ ] 11.1 Iniciar stack de observabilidade: `podman-compose -f docker-compose.observability.yml up -d`
- [ ] 11.2 Verificar logs de todos os serviços para erros
- [ ] 11.3 Validar health checks: Prometheus `/targets`, Loki `/ready`, Tempo `/ready`, Grafana `/api/health`
- [ ] 11.4 Verificar recebimento de traces no Tempo (via Grafana Explore)
- [ ] 11.5 Verificar recebimento de métricas no Prometheus (query: `up`)
- [ ] 11.6 Verificar recebimento de logs no Loki (query: `{service_name="api"}`)
- [ ] 11.7 Validar datasources configurados no Grafana
- [ ] 11.8 Testar correlação: buscar trace no Tempo, correlacionar com logs no Loki usando traceId

## 12. Testes de Integração

- [ ] 12.1 Fazer requisição à API e verificar trace completo no Tempo
- [ ] 12.2 Verificar métricas de HTTP aparecendo no Prometheus
- [ ] 12.3 Verificar logs estruturados aparecendo no Loki
- [ ] 12.4 Validar que profiling pode ser enviado ao Pyroscope (se aplicável)
- [ ] 12.5 Testar reinicialização da stack (dados devem persistir nos volumes)


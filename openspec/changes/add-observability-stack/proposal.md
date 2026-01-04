# Change: Adicionar Stack Completa de Observabilidade

## Why

O projeto MyNewLittleBank já possui OpenTelemetry Collector configurado e serviços .NET enviando telemetria via OTLP, mas não possui backends de observabilidade para armazenar, consultar e visualizar os dados coletados. A implementação de uma stack completa com Grafana, Prometheus, Loki, Tempo e Pyroscope permitirá:

- **Visualização unificada** de métricas, logs e traces através do Grafana
- **Armazenamento persistente** de métricas no Prometheus
- **Agregação centralizada** de logs no Loki
- **Tracing distribuído persistente** no Tempo (substituindo Jaeger em memória)
- **Profiling de performance** com Pyroscope para identificar gargalos

Esta stack complementa a infraestrutura existente sem quebrar a configuração atual do OpenTelemetry Collector.

## What Changes

- **ADDED**: Serviço Grafana como plataforma central de visualização
- **ADDED**: Serviço Prometheus para armazenamento e consulta de métricas time-series
- **ADDED**: Serviço Loki para agregação e consulta de logs estruturados
- **ADDED**: Serviço Tempo para armazenamento persistente de traces distribuídos
- **ADDED**: Serviço Pyroscope para profiling de performance e análise de CPU/memória
- **MODIFIED**: Configuração do OpenTelemetry Collector para rotear telemetria aos novos backends
- **ADDED**: Arquivos de configuração para todos os componentes da stack
- **ADDED**: Docker Compose para orquestração da stack de observabilidade
- **ADDED**: Documentação de configuração e uso

**Nota**: Dashboards do Grafana não serão criados nesta proposta, apenas a infraestrutura base.

## Impact

- **Affected specs**: Nova capability `observability-stack`
- **Affected code**:
  - `infra/docker-compose.yml` (atualização do collector)
  - `infra/config/otel-collector.yaml` (novos exporters)
  - Novos arquivos de configuração em `infra/config/`
  - Novo arquivo `infra/docker-compose.observability.yml`
- **Infrastructure**: Adiciona 5 novos containers (Grafana, Prometheus, Loki, Tempo, Pyroscope)
- **Ports**: Expõe novas portas (3000, 9090, 3100, 3200, 4040)
- **Storage**: Requer volumes persistentes para dados históricos
- **Dependencies**: Nenhuma mudança nos serviços .NET existentes

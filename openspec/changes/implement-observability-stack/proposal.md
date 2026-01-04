# Change: Implementar Stack de Observabilidade Completa

## Why

O projeto atualmente utiliza apenas o Aspire Dashboard para visualização de telemetria, o que limita as capacidades de observabilidade em produção. A implementação de uma stack completa com Jaeger (traces), Prometheus (métricas), Loki (logs) e Grafana (visualização unificada) permitirá:

- Rastreamento distribuído robusto entre microserviços
- Armazenamento persistente e consultas avançadas de métricas
- Centralização e correlação de logs estruturados
- Dashboards personalizados e alertas configuráveis
- Melhor capacidade de diagnóstico e troubleshooting em produção

Esta mudança é baseada no plano detalhado em `doc/Observabilidade/PLANO_OBSERVABILIDADE.md` e no repositório de referência [robersonnaves/Telemetry](https://github.com/robersonnaves/Telemetry).

## What Changes

- **ADDED**: Stack de observabilidade completa com OpenTelemetry Collector, Jaeger, Prometheus, Loki e Grafana
- **ADDED**: Configuração do OpenTelemetry Collector para rotear traces, métricas e logs para backends apropriados
- **ADDED**: Integração do Jaeger para visualização de traces distribuídos
- **ADDED**: Integração do Prometheus para coleta e armazenamento de métricas time-series
- **ADDED**: Integração do Loki para centralização de logs estruturados
- **ADDED**: Configuração do Grafana com datasources e dashboards pré-configurados
- **MODIFIED**: Configuração do OpenTelemetry Collector para exportar para múltiplos backends (não apenas Aspire)
- **MODIFIED**: Variáveis de ambiente dos serviços .NET para apontar para o Collector como endpoint único
- **ADDED**: Documentação de observabilidade atualizada com guias de uso

**BREAKING**: Nenhuma mudança breaking. A stack atual (Aspire) continuará funcionando, e a nova stack será adicionada em paralelo.

## Impact

- **Affected specs**: Nova capability `observability-stack`
- **Affected code**: 
  - `infra/docker-compose.yml` - Adição de serviços de observabilidade
  - `infra/docker-compose.observability.yml` - Novo arquivo com stack de observabilidade
  - `infra/config/otel-collector.yaml` - Atualização para múltiplos exporters
  - `infra/config/prometheus.yml` - Nova configuração
  - `infra/config/loki.yml` - Nova configuração
  - `infra/config/grafana/` - Novas configurações de datasources e dashboards
  - Variáveis de ambiente dos serviços .NET (via docker-compose)
- **Infrastructure**: Requer containers adicionais (Jaeger, Prometheus, Loki, Grafana)
- **Documentation**: Atualização de `infra/OBSERVABILITY.md` e criação de guias adicionais


# Design: Stack de Observabilidade

## Context

O projeto MyNewLittleBank já possui:
- OpenTelemetry Collector configurado e rodando
- Serviços .NET (.NET 8) instrumentados com OpenTelemetry
- Telemetria sendo enviada via OTLP gRPC (porta 4317) e HTTP (porta 4318)
- Extensão de observabilidade em `src/Shared/Observability/ObservabilityExtensions.cs`
- Documentação de planejamento em `doc/Observabilidade/`

**Gap identificado**: Falta infraestrutura de backends para armazenar, consultar e visualizar os dados coletados.

## Goals / Non-Goals

### Goals
- Implementar stack completa de observabilidade com componentes padrão da indústria
- Manter compatibilidade com configuração existente do OpenTelemetry Collector
- Fornecer visualização unificada através do Grafana
- Suportar armazenamento persistente de métricas, logs e traces
- Habilitar profiling de performance para análise de gargalos
- Configuração via Docker Compose para facilitar desenvolvimento local

### Non-Goals
- Criação de dashboards do Grafana (será feito em proposta separada)
- Configuração de alertas (será feito em proposta separada)
- Migração de dados existentes (não há dados históricos)
- Configuração de autenticação/segurança (ambiente de desenvolvimento)
- Otimizações de performance ou sampling (pode ser feito posteriormente)

## Decisions

### Decision 1: Usar Tempo ao invés de Jaeger
**Rationale**: 
- Tempo é parte do ecossistema Grafana (LGTM stack)
- Integração nativa com Grafana para correlação de traces, logs e métricas
- Armazenamento persistente por padrão (vs Jaeger em memória)
- Suporte completo a OTLP
- Melhor correlação entre sinais de observabilidade

**Alternatives considered**:
- Jaeger: Mais maduro, mas requer configuração adicional para persistência e não integra nativamente com Grafana
- OpenSearch: Mais pesado, requer mais recursos, overkill para este caso

### Decision 2: Stack Separada em docker-compose.observability.yml
**Rationale**:
- Separação de responsabilidades: stack principal vs observabilidade
- Facilita iniciar/parar stack de observabilidade independentemente
- Reduz complexidade do docker-compose.yml principal
- Permite escalar observabilidade separadamente

**Alternatives considered**:
- Tudo em um único docker-compose.yml: Mais simples, mas arquivo muito grande e difícil de manter
- Extends do docker-compose: Funcional, mas menos explícito

### Decision 3: Pyroscope para Profiling
**Rationale**:
- Integração nativa com Grafana
- Suporte a profiling de CPU e memória
- Interface web para análise de dados
- Suporte a múltiplos formatos de profiling (pprof, JFR, etc)
- Open source e parte do ecossistema Grafana

**Alternatives considered**:
- Datadog APM: Solução comercial, requer conta e configuração externa
- Jaeger Profiling: Limitado, não é foco principal do Jaeger

### Decision 4: Configuração via Arquivos YAML
**Rationale**:
- Padrão da indústria para ferramentas de observabilidade
- Versionamento de configuração no Git
- Facilita ajustes e troubleshooting
- Permite reutilização entre ambientes

**Alternatives considered**:
- Variáveis de ambiente apenas: Menos flexível, difícil de manter configurações complexas
- Configuração via API: Requer scripts adicionais, menos transparente

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│              Aplicações .NET                            │
│  API • Services.Card • Services.Money • Services.Pix   │
│  Services.Heartbeats • Mock.Transactions                │
└──────────────────┬──────────────────────────────────────┘
                    │ OTLP gRPC (4317)
                    │ OTLP HTTP (4318)
                    ▼
┌──────────────────────────────────────┐
│   OpenTelemetry Collector            │
│   • Recebe OTLP                      │
│   • Processa e enriquece             │
│   • Roteia para backends             │
└───┬────────────┬─────────────┬───────┘
    │            │             │
    ▼            ▼             ▼
┌────────┐  ┌─────────┐  ┌─────────┐
│ Tempo  │  │Promethe │  │  Loki   │
│(Traces)│  │ (Metrics│  │ (Logs)  │
└───┬────┘  └────┬────┘  └────┬────┘
    │            │             │
    └────────────┴─────────────┴──────┐
                                      │
                          ┌───────────▼─────────┐
                          │    Grafana          │
                          │ (Visualização)      │
                          └─────────────────────┘
                                      │
                                      ▼
                          ┌─────────────────────┐
                          │    Pyroscope        │
                          │   (Profiling)      │
                          └─────────────────────┘
```

### Componentes

1. **Grafana** (porta 3000)
   - Plataforma central de visualização
   - Datasources: Prometheus, Loki, Tempo
   - Auto-provisioning via arquivos YAML

2. **Prometheus** (porta 9090)
   - Scrape de métricas do OTel Collector (porta 8889)
   - Retenção: 7 dias (configurável)
   - Storage em volume persistente

3. **Loki** (porta 3100)
   - Recebe logs via OTLP HTTP do Collector
   - Schema v13 com TSDB
   - Retenção: 7 dias (configurável)

4. **Tempo** (porta 3200)
   - Recebe traces via OTLP gRPC do Collector
   - Storage em volume persistente
   - Suporte a queries via Grafana

5. **Pyroscope** (porta 4040)
   - Recebe dados de profiling via HTTP
   - Integração com Grafana para visualização
   - Suporte a múltiplos formatos

## Risks / Trade-offs

### Risk 1: Uso de Recursos
**Mitigation**: 
- Configurar limites de memória nos containers
- Retenção de dados limitada (7 dias)
- Monitorar uso de recursos durante desenvolvimento

### Risk 2: Complexidade de Configuração
**Mitigation**:
- Documentação detalhada
- Arquivos de configuração bem comentados
- Validação de configuração no startup

### Risk 3: Dependências entre Serviços
**Mitigation**:
- Usar `depends_on` no docker-compose
- Health checks configurados
- Retry logic no Collector para export failures

### Risk 4: Volume de Dados
**Mitigation**:
- Retenção limitada inicialmente
- Configuração de sampling pode ser adicionada posteriormente
- Monitoramento de uso de disco

## Migration Plan

### Fase 1: Preparação
1. Criar estrutura de diretórios `infra/config/`
2. Criar arquivos de configuração base
3. Criar `docker-compose.observability.yml`

### Fase 2: Configuração do Collector
1. Atualizar `infra/config/otel-collector.yaml` com novos exporters
2. Adicionar pipelines para Tempo, Loki, Pyroscope
3. Manter compatibilidade com configuração existente

### Fase 3: Deploy
1. Iniciar stack de observabilidade
2. Validar recebimento de dados
3. Configurar datasources no Grafana

### Fase 4: Validação
1. Verificar traces no Tempo
2. Verificar métricas no Prometheus
3. Verificar logs no Loki
4. Verificar profiling no Pyroscope
5. Validar visualização no Grafana

### Rollback
- Parar stack de observabilidade: `podman-compose -f docker-compose.observability.yml down`
- Reverter mudanças no `otel-collector.yaml` se necessário
- Stack principal continua funcionando normalmente

## Open Questions

### Question 1: Retenção de Dados para Desenvolvimento
**Decisão**: **7 dias de retenção**

**Justificativa**:
- Permite análise de padrões semanais
- Suficiente para debug de problemas intermitentes
- Balanceamento adequado entre histórico útil e uso de recursos

**Configuração**:
- Prometheus: `--storage.tsdb.retention.time=7d`
- Loki: `limits_config.retention_period: 168h` (7 dias)
- Tempo: `retention_period: 168h`
- Pyroscope: `retention: 7d`

**Monitoramento**: Adicionar alerta futuro se uso de disco > 80% do volume.

---

### Question 2: Sampling de Traces
**Decisão**: **Não configurar sampling inicialmente**

**Justificativa**:
- Em desenvolvimento, volume de traces é gerenciável
- Captura completa permite análise detalhada e debug
- Sampling pode ser adicionado posteriormente se volume aumentar

**Implementação**:
- Não adicionar processors de sampling no Collector inicialmente
- Documentar como habilitar sampling no futuro (probabilistic sampling como opção)
- Considerar habilitar sampling quando volume > 1000 traces/minuto

**Métrica de Monitoramento**: Monitorar taxa de traces recebidos no Tempo. Se > 1000/min, considerar habilitar sampling.

---

### Question 3: Labels e Metadados Automáticos
**Decisão**: **Incluir labels de negócio e observabilidade**

**Labels Base (já configurados)**:
- `service_name`: Nome do serviço
- `deployment.environment`: Ambiente (local, dev, prod)
- `service.version`: Versão do serviço

**Labels Adicionais a Incluir**:

1. **Labels de Negócio**:
   - `transaction.type`: Tipo de transação (Pix, Money, Card)
     - Adicionado via instrumentation nos serviços
     - Útil para análise de negócio e filtragem
     - Cardinalidade baixa (3-5 valores)
   
   - `account.id`: ID da conta (parcialmente mascarado para privacidade)
     - Adicionado via instrumentation nos serviços
     - **Atenção**: Alta cardinalidade - considerar mascaramento ou hash
     - Útil para rastreamento de transações por conta
     - **Recomendação**: Usar hash ou últimos 4 dígitos para reduzir cardinalidade

2. **Labels de Observabilidade**:
   - `trace.sampling.policy`: Política de sampling aplicada (quando habilitado)
     - Valor padrão: `always_on` (quando sampling não está ativo)
     - Útil para análise de representatividade quando sampling estiver habilitado
   
   - `error`: Boolean indicando se há erro no trace/span
     - Adicionado automaticamente pelo Collector baseado em status codes
     - Valores: `true`, `false`
     - Útil para filtrar erros rapidamente em queries

**Implementação**:
- Configurar `resource` processor no Collector para garantir labels consistentes
- Adicionar `attributes` processor para derivar `error` de status codes HTTP
- Documentar que `transaction.type` e `account.id` devem ser adicionados via instrumentation nos serviços .NET
- Configurar `trace.sampling.policy` no resource attributes quando sampling estiver habilitado

**Atenção - Cardinalidade**:
- `account.id` pode ter alta cardinalidade. Considerar:
  - Usar hash do account.id (ex: `account.id.hash`)
  - Usar últimos 4 dígitos apenas
  - Ou usar como atributo (não label) em traces/logs para evitar problemas de cardinalidade no Prometheus

**Métrica de Monitoramento**: Monitorar cardinalidade de labels no Prometheus. Alertar se > 100 valores únicos por label.

---

### Question 4: Configuração de Compressão e Otimização
**Decisão**: **Não configurar compressão inicialmente**

**Justificativa**:
- Overhead de CPU não justificado em desenvolvimento
- Volume de dados é gerenciável sem compressão
- Pode ser adicionado como otimização futura se necessário

---

### Question 5: Health Checks e Dependências
**Decisão**: **Implementar health checks completos**

**Configuração**:
- Usar `depends_on` com `condition: service_healthy` onde aplicável
- Configurar health checks apropriados para cada serviço
- Collector deve ter retry logic para export failures (já implementado no OTel)

**Ordem de Inicialização**:
1. Backends (Prometheus, Loki, Tempo, Pyroscope)
2. OpenTelemetry Collector (aguarda backends)
3. Grafana (aguarda todos os backends)

**Impacto na Implementação**: Adicionar health checks no `docker-compose.observability.yml` para todos os serviços.

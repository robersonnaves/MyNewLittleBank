# Design: Stack de Observabilidade Completa

## Context

O projeto MyNewLittleBank atualmente utiliza uma arquitetura simplificada de observabilidade onde todos os dados de telemetria (traces, métricas e logs) são enviados via OpenTelemetry Collector apenas para o Aspire Dashboard. Embora funcional para desenvolvimento, esta abordagem limita as capacidades em ambientes de produção que requerem:

- Armazenamento persistente de dados históricos
- Consultas avançadas e alertas configuráveis
- Visualização unificada de múltiplos sinais de telemetria
- Correlação entre traces, métricas e logs

## Goals / Non-Goals

### Goals
- Implementar stack completa de observabilidade com Jaeger, Prometheus, Loki e Grafana
- Manter compatibilidade com a stack atual (Aspire Dashboard)
- Configurar OpenTelemetry Collector como ponto central de coleta
- Habilitar correlação entre traces, métricas e logs via TraceId
- Fornecer dashboards pré-configurados para visualização
- Documentar configuração e uso da stack

### Non-Goals
- Substituir completamente o Aspire Dashboard (manter em paralelo)
- Implementar AlertManager nesta fase (futura melhoria)
- Configurar storage persistente para Jaeger (usar memória inicialmente)
- Implementar Grafana Tempo (futura melhoria)
- Configurar service mesh (fora do escopo)

## Decisions

### Decision: OpenTelemetry Collector como Hub Central
**What**: Usar o OpenTelemetry Collector como único ponto de entrada para toda telemetria, roteando para backends apropriados.

**Why**: 
- Padroniza a configuração de exportação nos serviços .NET
- Permite processamento e enriquecimento centralizado de dados
- Facilita adição/remoção de backends sem alterar código dos serviços
- Alinha com padrões OpenTelemetry

**Alternatives considered**:
- Exportação direta dos serviços para cada backend: Rejeitado por complexidade de configuração e manutenção
- Usar apenas Aspire: Rejeitado por limitações em produção

### Decision: Stack Separada em docker-compose.observability.yml
**What**: Criar arquivo separado `docker-compose.observability.yml` para serviços de observabilidade.

**Why**:
- Permite iniciar/parar stack de observabilidade independentemente
- Facilita manutenção e atualizações
- Mantém `docker-compose.yml` focado nos serviços de aplicação
- Permite uso opcional da stack (desenvolvimento pode usar apenas Aspire)

**Alternatives considered**:
- Integrar tudo em `docker-compose.yml`: Rejeitado por aumentar complexidade e acoplamento
- Usar docker-compose extends: Considerado, mas arquivo separado é mais explícito

### Decision: Jaeger com Storage em Memória
**What**: Configurar Jaeger usando `all-in-one` com storage em memória.

**Why**:
- Simplicidade para desenvolvimento e testes
- Reduz dependências externas (Elasticsearch/Cassandra)
- Adequado para ambientes de desenvolvimento

**Migration path**: Em produção, migrar para storage persistente (Elasticsearch ou Cassandra) conforme necessidade.

### Decision: Prometheus com Retention de 7 Dias
**What**: Configurar Prometheus com retenção de 7 dias para desenvolvimento.

**Why**:
- Balanceia necessidade de dados históricos com uso de recursos
- Adequado para desenvolvimento e testes
- Pode ser ajustado conforme necessidade

**Migration path**: Em produção, aumentar retention e considerar remote storage (Thanos, Cortex) se necessário.

### Decision: Loki com Schema v13 e TSDB
**What**: Usar Loki com schema v13 e TSDB para melhor performance de queries.

**Why**:
- TSDB oferece melhor performance para queries de métricas de logs
- Schema v13 é a versão mais recente e estável
- Suporta structured metadata e pattern ingestion

### Decision: Grafana com Auto-Provisioning
**What**: Configurar Grafana com auto-provisioning de datasources e dashboards via arquivos YAML.

**Why**:
- Elimina configuração manual após deploy
- Permite versionamento de configurações
- Facilita reprodução em diferentes ambientes

## Risks / Trade-offs

### Risk: Aumento de Uso de Recursos
**Mitigation**: 
- Stack pode ser iniciada apenas quando necessário
- Configurar limites de memória nos containers
- Usar storage em memória para Jaeger (desenvolvimento)

### Risk: Complexidade de Configuração
**Mitigation**:
- Documentação detalhada em `infra/OBSERVABILITY.md`
- Configurações versionadas em `infra/config/`
- Scripts de validação e health checks

### Risk: Incompatibilidade com Stack Atual
**Mitigation**:
- Manter Aspire Dashboard funcionando em paralelo
- Collector exporta para ambos (Aspire e novos backends)
- Migração gradual sem breaking changes

### Trade-off: Storage Persistente vs Simplicidade
**Decision**: Iniciar com storage em memória/volumes simples para desenvolvimento, documentar migração para produção.

## Migration Plan

### Fase 1: Preparação
- Backup de configurações atuais
- Criar estrutura de diretórios
- Parar containers atuais

### Fase 2: Configuração
- Configurar OpenTelemetry Collector com múltiplos exporters
- Configurar Jaeger, Prometheus, Loki
- Configurar Grafana com datasources

### Fase 3: Integração
- Atualizar variáveis de ambiente dos serviços .NET
- Verificar que telemetria flui para todos os backends
- Validar correlação entre sinais

### Fase 4: Validação
- Testar fluxo completo de dados
- Verificar dashboards no Grafana
- Validar queries em Prometheus e Loki
- Testar busca de traces no Jaeger

### Fase 5: Documentação
- Atualizar `infra/OBSERVABILITY.md`
- Criar guias de troubleshooting
- Documentar queries úteis

### Rollback Plan
- Parar stack de observabilidade: `podman-compose -f docker-compose.observability.yml down`
- Reverter mudanças no Collector para exportar apenas para Aspire
- Serviços .NET continuam funcionando normalmente

## Open Questions

- [ ] Definir estratégia de sampling de traces para produção (atualmente 100%)
- [ ] Decidir sobre AlertManager em fase futura
- [ ] Avaliar necessidade de Grafana Tempo para traces persistentes
- [ ] Definir estratégia de backup de dados do Prometheus e Loki


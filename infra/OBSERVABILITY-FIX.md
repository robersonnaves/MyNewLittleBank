# Correção do Conflito de Portas na Stack de Observabilidade

## Problema Identificado

A stack de observabilidade (`docker-compose.observability.yml`) não conseguia subir devido a **conflitos de porta** com a stack principal (`docker-compose.yml`):

### Conflito de Portas
- **otel-collector** (stack principal) expõe portas: `4317` (OTLP gRPC) e `4318` (OTLP HTTP)
- **tempo** (stack observabilidade) tentava expor as mesmas portas: `4317` e `4318`
- Docker/Podman não pode vincular a mesma porta do host a múltiplos containers simultaneamente

### Problema de Rede
- Ambos os compose files definiam `bank-net` de forma independente
- Causava conflito ao tentar criar redes com o mesmo nome

## Arquitetura Correta

```
Aplicações → otel-collector:4317/4318 → tempo:4317 (interno)
                                      → prometheus:8889
                                      → loki (http://loki/otlp)
```

**Ponto Chave**: Apenas o `otel-collector` deve expor portas OTLP para o host. O Tempo recebe traces internamente via rede Docker.

## Alterações Implementadas

### 1. docker-compose.observability.yml

#### Alteração 1: Remoção de Portas do Tempo
```yaml
# ANTES
tempo:
  ports:
    - "3200:3200"   # Tempo UI/API
    - "4317:4317"   # OTLP gRPC receiver ❌ CONFLITO
    - "4318:4318"   # OTLP HTTP receiver ❌ CONFLITO

# DEPOIS
tempo:
  ports:
    - "3200:3200"   # Tempo UI/API
    # OTLP ports (4317, 4318) são expostas pelo otel-collector na stack principal
    # Tempo recebe traces internamente via rede Docker (tempo:4317)
```

#### Alteração 2: Rede Externa
```yaml
# ANTES
networks:
  bank-net:    
    driver: bridge

# DEPOIS
networks:
  bank-net:
    external: true  # Usa rede criada pela stack principal (docker-compose.yml)
```

### 2. infra/scripts/start-all.ps1

Adicionado suporte para iniciar a stack de observabilidade automaticamente:

```powershell
# Start observability stack
Write-Host "`nStarting observability stack..." -ForegroundColor Cyan
$ObservabilityComposeFile = Join-Path $ScriptDir '..\docker-compose.observability.yml'
& $Engine compose -f $ObservabilityComposeFile -p "${ProjectName}-observability" up -d

if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Failed to start observability stack" -ForegroundColor Yellow
} else {
    Write-Host "Observability stack started successfully" -ForegroundColor Green
}

Write-Host "`nObservability stack status:" -ForegroundColor Cyan
& $Engine compose -f $ObservabilityComposeFile -p "${ProjectName}-observability" ps
```

## Como Usar

### Iniciar Todas as Stacks

```powershell
cd infra/scripts
.\start-all.ps1
```

Este comando agora:
1. Inicia a stack principal (PostgreSQL, RabbitMQ, otel-collector, serviços .NET, etc.)
2. Aguarda os serviços estarem prontos
3. Inicia a stack de observabilidade (Grafana, Prometheus, Loki, Tempo, Pyroscope)
4. Exibe o status de ambas as stacks

### Iniciar Manualmente (Método Alternativo)

```powershell
# 1. Stack principal
cd infra
podman compose up -d

# 2. Stack de observabilidade
podman compose -f docker-compose.observability.yml up -d
```

### Verificar Status

```powershell
# Stack principal
podman compose ps

# Stack observabilidade
podman compose -f docker-compose.observability.yml ps
```

### Parar as Stacks

```powershell
# Parar tudo
podman compose down
podman compose -f docker-compose.observability.yml down

# Ou usar o script
cd infra/scripts
.\stop-all.sh  # ou .\stop-all.ps1 se existir
```

## URLs de Acesso

Após iniciar as stacks, os serviços estão disponíveis em:

| Serviço | URL | Credenciais |
|---------|-----|-------------|
| **Grafana** | http://localhost:3000 | admin / admin |
| **Prometheus** | http://localhost:9090 | - |
| **Tempo UI** | http://localhost:3200 | - |
| **Loki** | http://localhost:3100 | - |
| **Pyroscope** | http://localhost:4040 | - |
| **otel-collector** | http://localhost:4317 (gRPC)<br>http://localhost:4318 (HTTP) | - |

## Fluxo de Telemetria

```
┌─────────────────┐
│  Aplicações     │
│  (.NET Services)│
└────────┬────────┘
         │ OTLP (4317/4318)
         ▼
┌─────────────────┐
│ otel-collector  │ ← Stack Principal
│ (host:4317/4318)│
└────────┬────────┘
         │
    ┌────┴────────────────────┐
    │                         │
    ▼                         ▼
┌─────────┐            ┌───────────┐
│  Tempo  │            │Prometheus │
│(interno)│            │  :8889    │
└─────────┘            └───────────┘
    │
    ▼
┌─────────┐
│ Grafana │
│  :3000  │
└─────────┘
```

## Validação

### 1. Verificar Portas
```powershell
# Verificar que apenas otel-collector expõe 4317/4318
podman ps --format "table {{.Names}}\t{{.Ports}}" | Select-String "431"

# Deve mostrar apenas: otel-collector com portas 4317 e 4318
```

### 2. Verificar Conectividade
```powershell
# Testar endpoint Grafana
curl http://localhost:3000/api/health

# Testar Prometheus
curl http://localhost:9090/-/ready

# Testar Tempo
curl http://localhost:3200/ready
```

### 3. Verificar Logs do otel-collector
```powershell
podman logs otel-collector

# Deve mostrar exportação bem-sucedida para Tempo:
# "Trace exported successfully to tempo:4317"
```

## Troubleshooting

### Erro: "port is already allocated"
- **Causa**: Stack principal não está rodando ou há outro processo usando as portas
- **Solução**: 
  ```powershell
  # Parar tudo
  podman compose down
  podman compose -f docker-compose.observability.yml down
  
  # Verificar processos nas portas
  netstat -ano | Select-String "4317|4318|3000|9090"
  
  # Reiniciar na ordem correta
  podman compose up -d
  podman compose -f docker-compose.observability.yml up -d
  ```

### Erro: "network bank-net not found"
- **Causa**: Stack principal não criou a rede `bank-net`
- **Solução**: Iniciar stack principal primeiro
  ```powershell
  podman compose up -d
  ```

### Erro: "unable to get image"
- **Causa**: Problema de conectividade com Podman machine
- **Solução**:
  ```powershell
  podman machine stop
  podman machine start
  ```

### Grafana não mostra dados
- **Causa**: Datasources não configurados ou serviços não alcançáveis
- **Solução**: 
  1. Verificar se todos os containers estão healthy: `podman ps`
  2. Verificar logs: `podman logs grafana`
  3. Acessar Grafana → Configuration → Data Sources
  4. Testar conectividade com Tempo, Prometheus, Loki

## Referências

- [Configuração OTEL Collector](./config/otel-collector.yaml)
- [Configuração Tempo](./config/tempo.yml)
- [Documentação Observabilidade](./OBSERVABILITY.md)
- [Guia Quick Start](../doc/Observabilidade/QUICK_START_OBSERVABILITY.md)

## Status do Task Master

Relacionado à tarefa **11.1** do planejamento de observabilidade:
- ✅ Conflito de portas corrigido
- ✅ Rede compartilhada configurada
- ✅ Script de inicialização atualizado
- ✅ Documentação criada

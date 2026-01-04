# Design: Worker Go para recepção de notificações

## Context
O ecossistema atual é baseado em .NET 8 com microsserviços containerizados, PostgreSQL, RabbitMQ e stack completa de observabilidade (OpenTelemetry Collector → Jaeger/Prometheus). Os workers .NET existentes (`Services.Card`, `Services.Money`, `Services.Pix`) processam transações de filas RabbitMQ. A API principal (`API`) já envia notificações HTTP via `HttpNotificationSender` para `{BaseUrl}/alerts/insufficient-funds`, mas o receptor ainda não existe. Esta mudança introduz o primeiro componente Go no projeto.

## Goals / Non-Goals

**Goals:**
- Criar worker HTTP em Go que recebe notificações de saldo insuficiente.
- Integrar com OpenTelemetry Collector existente (OTLP gRPC na porta 4317).
- Persistir notificações como arquivos JSON em disco para auditoria inicial.
- Containerizar e integrar ao `docker-compose.yml` existente.
- Seguir melhores práticas Go: módulos, structured logging (slog), context propagation, graceful shutdown.

**Non-Goals:**
- Implementar envio real de notificações (email, SMS, push) – futuro.
- Armazenar notificações em banco de dados – fase inicial usa apenas arquivos.
- Migrar workers .NET existentes para Go.
- Implementar autenticação/autorização no endpoint.
- Suportar outros tipos de notificações além de `insufficient_funds`.

## Decisions

### Linguagem e Runtime
**Decisão**: Go 1.22+ com módulos.  
**Justificativa**: Go oferece simplicidade, concorrência nativa, binários estáticos leves e ótima integração com OpenTelemetry. Escolhemos Go em vez de .NET para demonstrar interoperabilidade e preparar o terreno para workers de alta concorrência no futuro.  
**Alternativas consideradas**: .NET (consistência com a stack, mas adiciona overhead de runtime e imagem maior).

### Estrutura de Diretórios
```
workers/
└── notification-receiver-go/
    ├── cmd/
    │   └── server/
    │       └── main.go          # Entrypoint
    ├── internal/
    │   ├── handler/
    │   │   └── alerts.go        # HTTP handler
    │   ├── storage/
    │   │   └── filewriter.go    # Persistência JSON
    │   └── telemetry/
    │       └── otel.go          # OpenTelemetry setup
    ├── Dockerfile
    ├── go.mod
    ├── go.sum
    └── README.md
```
**Justificativa**: Segue convenções Go padrão (`cmd/` para entrypoints, `internal/` para lógica privada). Estrutura simples adequada para um microserviço single-purpose.

### API HTTP
- **Framework**: `net/http` padrão + `chi` (router leve) para roteamento e middlewares.
- **Endpoint**: `POST /alerts/insufficient-funds`.
- **Payload**: JSON matching `InsufficientFundsNotification` do .NET:
  ```json
  {
    "cpf": "12345678901",
    "accountNumber": "10000001",
    "transactionId": "550e8400-e29b-41d4-a716-446655440000",
    "attemptedAmount": 1500.50,
    "availableBalance": 500.00,
    "occurredAt": "2025-12-17T10:30:00Z",
    "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
    "reason": "insufficient_funds"
  }
  ```
- **Headers**: `Content-Type: application/json`, `X-Correlation-Id` (opcional, usado para propagação de trace).
- **Respostas**:
  - `202 Accepted`: notificação recebida e persistida.
  - `400 Bad Request`: payload inválido.
  - `500 Internal Server Error`: falha ao persistir.

**Justificativa**: `chi` oferece middlewares prontos (logging, recovery, CORS se necessário) sem overhead de frameworks pesados. Código idiomatic Go.

### Persistência Inicial
**Decisão**: Arquivos JSON individuais em diretório configurável via env var `NOTIFICATIONS_DIR` (default: `/data/notifications`).  
**Formato de arquivo**: `{transactionId}.json` (apenas o UUID da transação).  
**Idempotência**: Antes de persistir, verificar se arquivo com o `transactionId` já existe. Se existir, retornar 202 Accepted sem reescrever (idempotente). Se não existir, criar o arquivo.  
**Justificativa**: Formato simplificado facilita busca por transação específica. Idempotência garante que retries do sender (.NET) não criem duplicatas e permitem safe retries. Auditoria imediata e troubleshooting simplificados.  
**Alternativas consideradas**: Arquivo com timestamp (descartado – gera duplicatas em retry), banco de dados (overhead prematuro), append a um único arquivo (parsing difícil, lock contention).

### Observabilidade
- **Traces**: OpenTelemetry SDK para Go exportando OTLP gRPC para `otel-collector:4317`.
  - Extrai `traceparent` do header `X-Correlation-Id` (W3C Trace Context) enviado pelo .NET.
  - Cria spans para operações HTTP e I/O de arquivo.
- **Métricas**: Contador de notificações recebidas, histograma de latência.
- **Logs**: `log/slog` com formato JSON incluindo trace/span IDs.

**Justificativa**: Integração nativa com stack existente, mantém rastreabilidade end-to-end.

### Configuração
Via variáveis de ambiente:
- `PORT` (default: `8080`): porta do servidor HTTP.
- `NOTIFICATIONS_DIR` (default: `/data/notifications`): diretório de persistência.
- `OTEL_EXPORTER_OTLP_ENDPOINT` (default: `otel-collector:4317`): endpoint do Collector.
- `OTEL_SERVICE_NAME` (default: `notification-receiver`): nome do serviço em traces.
- `OTEL_RESOURCE_ATTRIBUTES` (ex: `deployment.environment=local`).

**Justificativa**: Padrão OpenTelemetry e convenções container-native.

### Containerização
**Base image**: `golang:1.22-alpine` para build, `scratch` ou `alpine:latest` para runtime.  
**Multi-stage build**: reduz tamanho final (binário estático Go ~10-20MB).  
**Volumes**: Named volume `notification_data` montado em `/data/notifications` no container para persistir arquivos fora do container e facilitar gestão pelo Docker/Podman.

## Risks / Trade-offs

**Risco**: Arquivos em disco podem crescer indefinidamente.  
**Mitigação**: Adicionar housekeeping (rotation/archive) em iteração futura; documentar limpeza manual.

**Risco**: Falta de autenticação permite envios espúrios.  
**Mitigação**: Ambiente local/trusted network na fase inicial; adicionar API key ou mTLS em produção.

**Risco**: Introduzir Go diversifica a stack tecnológica.  
**Mitigação**: Justificado por performance/concorrência; documentar bem e treinar equipe se necessário.

**Trade-off**: JSON files vs banco de dados.  
**Escolha**: Files para MVP (simplicidade); migrar para DB quando volume justificar ou quando adicionar query/analytics.

## Migration Plan

1. Implementar worker Go com testes unitários.
2. Adicionar serviço ao `docker-compose.yml` com volume mapeado.
3. Configurar `HttpNotificationSender` (.NET) com `BaseUrl` apontando para `http://notification-receiver:8080`.
4. Testar integração end-to-end: provocar transação com saldo insuficiente, verificar arquivo JSON gerado e traces no Jaeger.
5. Documentar formato de arquivo e localização no README do worker.

**Rollback**: Remover entrada do `docker-compose.yml` e restaurar config do .NET para desabilitar notificações (`EnableInsufficientFundsNotifications=false`).

## Resolved Design Questions

- **Q**: Precisa suportar retry/idempotência no receiver?  
  **A**: **SIM**. Implementar idempotência baseada em `transactionId`: verificar se arquivo já existe antes de persistir. Se existir, retornar 202 Accepted sem reescrever. Isso permite retries seguros do sender .NET e evita duplicatas.

- **Q**: Formato do nome do arquivo é suficiente para auditoria?  
  **A**: Formato simplificado para `{transactionId}.json` (sem timestamp). O `transactionId` é único e suficiente para auditoria. Timestamp está no payload (`occurredAt`). Formato simplificado facilita busca direta por transação.

- **Q**: Volume mapeado deve ser nomeado ou bind mount?  
  **A**: **Named volume** no docker-compose (`notification_data`), facilita gestão pelo Docker/Podman e evita problemas de permissões.

## ADDED Requirements

### Requirement: Recepção de Notificações de Saldo Insuficiente via API REST com Idempotência
O worker SHALL expor endpoint HTTP `POST /alerts/insufficient-funds` que recebe notificações de saldo insuficiente enviadas pelos serviços de transação (.NET), persiste cada notificação como arquivo JSON individual em disco usando `transactionId` como nome do arquivo, implementa idempotência verificando existência prévia do arquivo, e responde com status 202 Accepted em caso de sucesso ou duplicata.

#### Scenario: Notificação recebida com sucesso (primeira vez)
- **WHEN** uma requisição POST válida é enviada para `/alerts/insufficient-funds` com payload JSON contendo `cpf`, `accountNumber`, `transactionId`, `attemptedAmount`, `availableBalance`, `occurredAt`, `traceId` e `reason`
- **AND** o arquivo `{NOTIFICATIONS_DIR}/{transactionId}.json` NÃO existe
- **THEN** o worker retorna HTTP 202 Accepted
- **AND** persiste a notificação como arquivo JSON em `{NOTIFICATIONS_DIR}/{transactionId}.json`
- **AND** o arquivo contém todos os campos do payload recebido
- **AND** incrementa métrica `notifications_received_total{status="success"}`.

#### Scenario: Notificação duplicada (retry idempotente)
- **WHEN** uma requisição POST válida é enviada para `/alerts/insufficient-funds` com `transactionId` já processado anteriormente
- **AND** o arquivo `{NOTIFICATIONS_DIR}/{transactionId}.json` JÁ existe
- **THEN** o worker retorna HTTP 202 Accepted sem reescrever o arquivo
- **AND** loga evento de duplicata detectada em nível Info (não é erro)
- **AND** incrementa métrica `notifications_received_total{status="duplicate"}`.

#### Scenario: Payload inválido ou incompleto
- **WHEN** uma requisição POST é enviada com JSON malformado ou campos obrigatórios faltando (`cpf`, `accountNumber`, `transactionId`, `attemptedAmount`, `availableBalance`, `occurredAt`, `reason`)
- **THEN** o worker retorna HTTP 400 Bad Request com body descrevendo o erro de validação
- **AND** nenhum arquivo é criado
- **AND** incrementa métrica `notifications_received_total{status="invalid_payload"}`.

#### Scenario: Falha ao persistir arquivo
- **WHEN** uma requisição POST válida é recebida mas o sistema não consegue escrever no diretório de notificações (permissões, disco cheio)
- **THEN** o worker retorna HTTP 500 Internal Server Error
- **AND** registra erro detalhado em logs estruturados e telemetria
- **AND** incrementa métrica `notifications_received_total{status="storage_error"}`.

### Requirement: Propagação de Contexto de Rastreamento (Trace Context)
O worker SHALL integrar-se com OpenTelemetry para receber e propagar contexto de rastreamento distribuído, extraindo `traceparent` do header `X-Correlation-Id` (se presente) e criando spans para operações HTTP e I/O de arquivo, exportando traces via OTLP gRPC para o OpenTelemetry Collector.

#### Scenario: Trace context propagado corretamente
- **WHEN** uma requisição POST inclui header `X-Correlation-Id` com trace ID válido no formato W3C Trace Context
- **THEN** o worker extrai o trace ID e cria spans filhos associados ao trace recebido
- **AND** exporta os spans para `otel-collector:4317` via OTLP gRPC
- **AND** os spans aparecem no Jaeger correlacionados com o trace original da transação .NET.

#### Scenario: Sem trace context (cold start)
- **WHEN** uma requisição POST não inclui header `X-Correlation-Id` ou está vazio
- **THEN** o worker cria um novo trace root e spans independentes
- **AND** exporta normalmente para o Collector.

### Requirement: Métricas de Operação
O worker SHALL exportar métricas via OpenTelemetry incluindo contador de notificações recebidas (com label `status`: `success`, `duplicate`, `invalid_payload`, `storage_error`) e histograma de latência de processamento do endpoint.

#### Scenario: Métricas exportadas para Prometheus
- **WHEN** o worker processa notificações
- **THEN** incrementa contador `notifications_received_total{status="success"}` para notificações novas persistidas com sucesso
- **AND** incrementa `notifications_received_total{status="duplicate"}` para notificações duplicadas (idempotência)
- **AND** incrementa `notifications_received_total{status="invalid_payload"}` para erros de validação
- **AND** incrementa `notifications_received_total{status="storage_error"}` para falhas de persistência
- **AND** registra latência em histograma `notification_processing_duration_seconds`
- **AND** métricas ficam disponíveis no Prometheus via Collector endpoint `:8889`.

### Requirement: Configuração via Variáveis de Ambiente
O worker SHALL suportar configuração completa via variáveis de ambiente para porta do servidor, diretório de persistência e parâmetros de OpenTelemetry, permitindo operação zero-config com defaults sensatos.

#### Scenario: Configuração via environment variables
- **WHEN** o worker é iniciado com variáveis `PORT`, `NOTIFICATIONS_DIR`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`
- **THEN** utiliza os valores configurados para todas as operações
- **AND** loga a configuração efetiva no startup (omitindo secrets se houver).

#### Scenario: Defaults aplicados sem configuração explícita
- **WHEN** o worker é iniciado sem variáveis de ambiente configuradas
- **THEN** utiliza `PORT=8080`, `NOTIFICATIONS_DIR=/data/notifications`, `OTEL_EXPORTER_OTLP_ENDPOINT=otel-collector:4317`, `OTEL_SERVICE_NAME=notification-receiver`
- **AND** opera normalmente com esses valores.

### Requirement: Logs Estruturados com Contexto de Observabilidade
O worker SHALL emitir logs estruturados em formato JSON usando `log/slog` (Go standard library), incluindo trace ID e span ID em cada entrada de log, e registrando eventos-chave (startup, requisições recebidas, erros de persistência, shutdown).

#### Scenario: Logs incluem trace context
- **WHEN** uma requisição é processada com trace context ativo
- **THEN** todas as linhas de log geradas durante o processamento incluem campos `trace_id` e `span_id`
- **AND** logs ficam correlacionados com traces no Jaeger.

#### Scenario: Eventos de ciclo de vida logados
- **WHEN** o worker inicia
- **THEN** loga mensagem `service_started` com configuração efetiva
- **WHEN** o worker recebe sinal de shutdown (SIGTERM/SIGINT)
- **THEN** loga mensagem `shutting_down` e executa graceful shutdown aguardando requisições em andamento (timeout configurável).

### Requirement: Containerização e Integração com Docker Compose
O worker SHALL ser containerizado usando Dockerfile multi-stage com imagem base Alpine/scratch, expor porta configurável, montar volume para persistência de arquivos e integrar-se à rede `bank-net` no `docker-compose.yml` existente, com health check HTTP.

#### Scenario: Container sobe e responde health check
- **WHEN** o serviço `notification-receiver` é iniciado via `docker-compose up`
- **THEN** o container sobe na rede `bank-net` e expõe porta 8080 (mapeada para host conforme docker-compose)
- **AND** endpoint `/health` retorna HTTP 200 OK
- **AND** volume `notification_data` é montado em `/data/notifications` dentro do container.

#### Scenario: Comunicação com OpenTelemetry Collector
- **WHEN** o worker está rodando no Docker Compose
- **THEN** consegue resolver hostname `otel-collector` via DNS da rede `bank-net`
- **AND** exporta traces e métricas com sucesso para `otel-collector:4317`.

### Requirement: Formato de Arquivo JSON e Nomenclatura Simplificada
O worker SHALL persistir cada notificação como arquivo JSON individual no diretório configurado, nomeando cada arquivo exclusivamente pelo `transactionId` no formato `{transactionId}.json`, garantindo que cada transação tenha um único arquivo independentemente de retries.

#### Scenario: Arquivo criado com nome baseado em transactionId
- **WHEN** uma nova notificação é recebida e persistida
- **THEN** cria arquivo nomeado como `{transactionId}.json` (ex: `550e8400-e29b-41d4-a716-446655440000.json`)
- **AND** o conteúdo do arquivo é um objeto JSON com todos os campos do payload (`cpf`, `accountNumber`, `transactionId`, `attemptedAmount`, `availableBalance`, `occurredAt`, `traceId`, `reason`)
- **AND** o JSON é formatado (pretty-printed) para facilitar leitura humana.

#### Scenario: Idempotência via verificação de arquivo existente
- **WHEN** uma notificação é recebida para `transactionId` que já possui arquivo correspondente
- **THEN** o worker NÃO sobrescreve o arquivo existente
- **AND** retorna 202 Accepted normalmente (operação idempotente).

#### Scenario: Diretório criado automaticamente se não existir
- **WHEN** o worker inicia e `NOTIFICATIONS_DIR` não existe
- **THEN** cria o diretório recursivamente com permissões adequadas
- **AND** loga a criação do diretório.

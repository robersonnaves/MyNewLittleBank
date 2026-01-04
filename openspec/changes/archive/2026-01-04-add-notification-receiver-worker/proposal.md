# Change: Implementar worker Go para receber notificações de saldo insuficiente

## Why
O sistema já envia notificações HTTP de saldo insuficiente via `HttpNotificationSender` para o endpoint `/alerts/insufficient-funds`, mas o serviço receptor ainda não existe. É necessário implementar um worker que receba essas notificações e as persista inicialmente em disco como arquivos JSON, permitindo auditoria e futura integração com sistemas de notificação aos clientes (email, SMS, push).

## What Changes
- Criar worker em Go que expõe API REST para receber eventos de notificações de saldo insuficiente.
- Implementar endpoint `POST /alerts/insufficient-funds` que recebe payload com CPF, accountNumber, transactionId, attemptedAmount, availableBalance, occurredAt, traceId e reason.
- Integrar com OpenTelemetry para enviar traces/métricas ao Collector já existente.
- Persistir cada notificação recebida como arquivo JSON individual em diretório configurável.
- Containerizar com Dockerfile e adicionar ao `docker-compose.yml` da infraestrutura.

## Impact
- Affected specs: Novo spec `notification-receiver` (nova capacidade).
- Affected code: Novo projeto Go (`workers/notification-receiver-go/`), nova entrada no `docker-compose.yml`, configuração de volumes para persistência de arquivos.
- External dependencies: Go 1.22+, bibliotecas OpenTelemetry para Go, networking com `otel-collector` na rede `bank-net`.

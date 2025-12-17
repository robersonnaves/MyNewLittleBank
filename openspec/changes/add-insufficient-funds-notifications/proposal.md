# Change: Notificar saldo insuficiente sem exceção

## Why
Hoje o débito de conta com saldo insuficiente lança uma `DomainException` (`insufficient_funds`), quebra o fluxo e não gera alerta ao cliente ou à operação. Precisamos acionar uma API REST de notificações (a ser construída) nesses casos, enviando CPF e dados para rastreabilidade/observabilidade.

## What Changes
- Tratar saldo insuficiente sem lançar exceção, retornando falha estruturada no fluxo de transações.
- Acionar a API REST de notificações ao detectar saldo insuficiente, enviando CPF, motivo (`insufficient_funds`) e metadados para observabilidade (conta, transação, valor tentado, saldo atual, timestamp, trace/correlation id).
- Registrar telemetria do alerta e assegurar que falhas na chamada de notificação não derrubem o fluxo principal (mantendo o erro de negócio retornado).

## Impact
- Affected specs: account-services (novo requisito de alerta de saldo insuficiente)
- Affected code: Domain.BankAccount (débito), UseCases.ProcessTransactionsHandler, integração HTTP de notificações e configuração/telemetria associada.

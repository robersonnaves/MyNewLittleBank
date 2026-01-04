## 1. Implementação
- [x] 1.1 Atualizar `BankAccount.Debit` para retornar falha `insufficient_funds` em vez de lançar exceção e alinhar testes de domínio.
- [x] 1.2 Introduzir porta/DTO de notificação REST (UseCases) com payload `{ cpf, reason, accountNumber, transactionId, attemptedAmount, availableBalance, occurredAt, traceId/correlationId }`.
- [x] 1.3 Ajustar `ProcessTransactionsHandler` para acionar a notificação ao detectar saldo insuficiente, retornando falha de negócio e evitando persistir transação/outbox nesses casos.
- [x] 1.4 Implementar cliente HTTP mínimo para a API de notificações com observabilidade (logs/tracing) e timeout curto, parametrizando endpoint/configuração.
- [x] 1.5 Cobrir novos fluxos com testes (domínio + use case) garantindo que a notificação é chamada e que falhas de notificação não propagam exceção.

## 2. Validação
- [x] 2.1 Documentar o contrato/payload da notificação nas referências internas conforme o spec.
- [x] 2.2 Rodar `openspec validate add-insufficient-funds-notifications --strict`.

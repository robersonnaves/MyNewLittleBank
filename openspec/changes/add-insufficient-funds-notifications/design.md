## Context
Débitos de conta com saldo insuficiente hoje lançam `DomainException` (`insufficient_funds`), capturada nos handlers para retornar falha. Nenhuma ação de alerta é disparada e não existe integração de notificações. O novo fluxo deve acionar uma API REST de notificações (ainda a ser construída) com CPF do cliente, motivo e metadados de observabilidade, mantendo o erro de negócio sem derrubar o serviço.

## Goals / Non-Goals
- Goals: evitar exceções de domínio em saldo insuficiente; acionar API REST de notificações com CPF/motivo/metadados; manter rastreabilidade (trace/correlation id, payload da tentativa); garantir que falhas da notificação não quebrem o fluxo principal.
- Non-Goals: implementar o serviço de notificações em si; alterar outros motivos de falha de débito; mudar contratos públicos de transações além do tratamento de `insufficient_funds`; introduzir filas ou padrões adicionais além de REST síncrono.

## Decisions
- A checagem de saldo insuficiente passará a retornar `Result` de falha (`insufficient_funds`) em vez de lançar exceção, permitindo tratamento normalizado no handler.
- Introduzir uma porta de saída (ex.: `INotificationSender` em UseCases) para postar em uma API REST de notificações; a infraestrutura proverá o cliente HTTP com timeout curto e headers de correlação/trace.
- Payload mínimo da notificação: `cpf`, `reason` (fixo `insufficient_funds`), `accountNumber`, `transactionId`, `attemptedAmount`, `availableBalance`, `occurredAt`, `traceId` (ou correlation id reutilizando tracing OpenTelemetry existente).
- O handler de transações invocará a notificação somente quando a operação de débito retornar falha de saldo insuficiente; nenhuma persistência de transação é realizada nesse caso, mas a tentativa é registrada em telemetria/log.
- Falha na chamada REST não altera o resultado de negócio (`insufficient_funds`); o erro é logado/observado para troubleshooting, sem retries internos nesta fase.

## Risks / Trade-offs
- Chamada REST síncrona adiciona latência ao caminho de falha; mitigação: timeout curto e envio minimalista.
- Dependência de obter CPF a partir do `ClientId` da conta; mitigar com leitura dedicada do cliente antes da chamada e cache leve se necessário.
- Ausência de API de notificações pronta pode atrasar validação end-to-end; mitigar com cliente stub ou contrato documentado + testes usando handler fake.

## Open Questions (Resolved)
- Endpoint REST específico: usar `/alerts/insufficient-funds`.
- Trace/correlation id para consumo externo: enviar via HTTP header dedicado (além do payload opcional).

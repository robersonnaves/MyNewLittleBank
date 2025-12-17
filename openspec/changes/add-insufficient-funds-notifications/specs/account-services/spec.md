## ADDED Requirements
### Requirement: Notificar saldo insuficiente via API REST
O serviço de contas SHALL acionar uma API REST de notificações quando um débito/pagamento for rejeitado por saldo insuficiente, substituindo a exceção por falha de negócio `insufficient_funds` e enviando CPF e metadados de observabilidade da tentativa.

#### Scenario: Débito recusado envia alerta REST
- **WHEN** uma operação de débito ou pagamento tenta retirar valor maior que o saldo disponível,
- **THEN** o sistema retorna falha de negócio `insufficient_funds` sem lançar exceção de domínio
- **AND** chama a API REST de notificações uma única vez com payload contendo CPF do cliente, número da conta, transactionId, valor solicitado, saldo disponível, timestamp do ocorrido, identificador de rastreamento (trace/correlation id) e motivo `insufficient_funds`.
- **AND** registra sucesso ou falha da chamada em telemetria/log para observabilidade.

#### Scenario: Falha na notificação não derruba fluxo
- **WHEN** a chamada REST de notificação falha por timeout ou resposta 5xx,
- **THEN** o sistema ainda retorna a falha `insufficient_funds` para a transação
- **AND** registra o erro em telemetria/log sem lançar exceção não tratada nem persistir transação/outbox associada.

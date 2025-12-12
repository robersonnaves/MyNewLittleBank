## ADDED Requirements

### Requirement: Mock.Transactions bloqueia payloads inválidos antes de publicar
O produtor `Mock.Transactions` SHALL validar os payloads de transação e NÃO publicar mensagens que violem regras mínimas de domínio (IDs vazios, chaves Pix vazias ou números de cartão ausentes), gerando log de erro para correção.

#### Scenario: TransactionId não pode ser vazio
- **WHEN** um payload de transação é construído para publicação,
- **THEN** se `TransactionId` for `Guid.Empty` ou ausente, a publicação é bloqueada e o erro é registrado.

#### Scenario: Chaves Pix não vazias e pré-existentes
- **WHEN** uma transação Pix é gerada,
- **THEN** as chaves de origem e destino são selecionadas de chaves Pix pré-cadastradas e não vazias; caso contrário, a publicação é bloqueada e o erro é registrado.

#### Scenario: Número de cartão obrigatório
- **WHEN** uma transação de cartão é gerada,
- **THEN** o número de cartão deve estar presente e não vazio; caso contrário, a publicação é bloqueada e o erro é registrado.

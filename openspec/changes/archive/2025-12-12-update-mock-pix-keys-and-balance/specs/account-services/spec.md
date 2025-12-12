## ADDED Requirements

### Requirement: Persistir saldo de conta após processar transações
O sistema SHALL atualizar e persistir o saldo das contas bancárias na tabela `bank_accounts` sempre que transações de crédito ou débito forem processadas com sucesso.

#### Scenario: Saldo é debitado e salvo
- **WHEN** uma transação de débito é processada com sucesso para uma conta existente,
- **THEN** o saldo da conta é reduzido e o novo valor é persistido no banco na tabela `bank_accounts`.

#### Scenario: Saldo é creditado e salvo
- **WHEN** uma transação de crédito é processada com sucesso para uma conta existente,
- **THEN** o saldo da conta é aumentado e o novo valor é persistido no banco na tabela `bank_accounts`.

# account-services Specification

## Purpose
TBD - created by archiving change create-core-api. Update Purpose after archive.
## Requirements
### Requirement: Check Account Balance
The system MUST allow querying the current balance of a specific bank account.

#### Scenario: Successfully check balance
- **Given** an existing Account ID.
- **When** the Get Balance endpoint is called.
- **Then** the current available balance is returned.

### Requirement: Get Account Details
The system MUST allow retrieving general account information.

#### Scenario: Retrieve account details
- **Given** an existing Account ID.
- **When** the Get Account endpoint is called.
- **Then** the account number, branch, and status are returned.

### Requirement: Persistir saldo de conta após processar transações
O sistema SHALL atualizar e persistir o saldo das contas bancárias na tabela `bank_accounts` sempre que transações de crédito ou débito forem processadas com sucesso.

#### Scenario: Saldo é debitado e salvo
- **WHEN** uma transação de débito é processada com sucesso para uma conta existente,
- **THEN** o saldo da conta é reduzido e o novo valor é persistido no banco na tabela `bank_accounts`.

#### Scenario: Saldo é creditado e salvo
- **WHEN** uma transação de crédito é processada com sucesso para uma conta existente,
- **THEN** o saldo da conta é aumentado e o novo valor é persistido no banco na tabela `bank_accounts`.


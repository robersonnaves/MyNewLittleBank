## ADDED Requirements

### Requirement: Mock.Transactions sempre gera payloads válidos
O produtor `Mock.Transactions` SHALL gerar apenas payloads de transação que atendam às regras mínimas de domínio antes de publicar, incluindo TransactionId não vazio, chaves Pix válidas e número de cartão presente/formatado.

#### Scenario: TransactionId gerado sempre válido
- **WHEN** um payload é criado para publicação,
- **THEN** o `TransactionId` é preenchido com um GUID não vazio.

#### Scenario: Chaves Pix válidas e não vazias
- **WHEN** uma transação Pix é gerada,
- **THEN** as chaves de origem e destino são selecionadas de um conjunto válido (telefone, email, CPF ou GUID) e nunca são vazias.

#### Scenario: Número de cartão presente
- **WHEN** uma transação de cartão é gerada,
- **THEN** o número de cartão está presente e formatado/normalizado antes da publicação.

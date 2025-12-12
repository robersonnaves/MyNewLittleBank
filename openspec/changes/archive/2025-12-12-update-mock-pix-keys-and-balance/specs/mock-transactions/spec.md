## ADDED Requirements

### Requirement: Mock.Transactions suporta múltiplos tipos de chaves Pix
O produtor `Mock.Transactions` SHALL gerar e utilizar chaves Pix válidas associadas às contas mockadas podendo ser telefone, email, CPF ou chave aleatória (GUID), escolhidas a partir do conjunto registrado para cada conta.

#### Scenario: Pix usa chaves de telefone ou email
- **WHEN** uma transação Pix é gerada,
- **THEN** as chaves de origem/destino podem ser selecionadas de telefones ou emails cadastrados para as contas mockadas e não podem ser vazias.

#### Scenario: Pix usa chave CPF
- **WHEN** uma transação Pix é gerada,
- **THEN** as chaves podem ser CPFs válidos associados às contas mockadas, sem valores vazios ou inválidos.

#### Scenario: Pix usa chave aleatória (GUID)
- **WHEN** uma transação Pix é gerada,
- **THEN** as chaves podem ser GUIDs previamente registrados para as contas mockadas, garantindo não-vazios e unicidade dentro do conjunto da conta.

# mock-transactions Specification

## Purpose
TBD - created by archiving change update-mock-transactions-precreate-clients-accounts. Update Purpose after archive.
## Requirements
### Requirement: Transações de mock usam clientes e contas registradas
O produtor `Mock.Transactions` SHALL pré-registrar ou reutilizar clientes e contas na API antes de emitir transações, e SHALL montar transações apenas com IDs dessas entidades registradas.

#### Scenario: Seed cria clientes e contas antes de publicar
- **WHEN** o serviço inicia e não possui clientes/contas alvo conhecidos,
- **THEN** ele chama a API (`src/API/API.csproj`) para criar a quantidade configurada de clientes e contas e só passa a publicar transações após receber sucesso e capturar os identificadores.

#### Scenario: Reutilização de registros existentes
- **WHEN** clientes ou contas alvo já existem na API (ex.: seed prévio),
- **THEN** o Mock.Transactions reutiliza esses registros sem duplicar cadastros, mantendo uma lista de IDs válidos para geração.

#### Scenario: Bloqueio seguro em falha de seed
- **WHEN** o seed não consegue criar ou recuperar clientes/contas (erro de API, autenticação, validação),
- **THEN** o Mock.Transactions não publica transações órfãs, registra o erro e aborta ou retenta o seed conforme configuração.

### Requirement: Seed cria 10 clientes com 1-3 contas cada
O `Mock.Transactions` SHALL criar exatamente 10 clientes durante o seed, atribuindo para cada um um número aleatório de contas entre 1 e 3 (inclusive) antes de publicar transações.

#### Scenario: Seed gera 10 clientes
- **WHEN** o serviço inicializa com seed habilitado,
- **THEN** ele cria (ou reutiliza) 10 clientes via API e mantém seus identificadores para uso nas transações.

#### Scenario: Cada cliente tem de 1 a 3 contas
- **WHEN** o seed cria contas para cada cliente,
- **THEN** cada cliente recebe aleatoriamente de 1 a 3 contas, garantindo pelo menos uma conta por cliente antes de qualquer publicação.

#### Scenario: Validação dos limites de seed
- **WHEN** a configuração do seed é carregada na inicialização,
- **THEN** valores fora dos limites (clientes ≠ 10, contas fora de 1-3) são rejeitados/validados, impedindo a publicação até correção.

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

### Requirement: Mock.Transactions usa chaves Pix já cadastradas
O serviço `Mock.Transactions` SHALL publicar transações Pix apenas com chaves Pix previamente cadastradas e vinculadas às contas alvo, alinhando o comportamento ao uso de contas já cadastradas em transações em dinheiro.

#### Scenario: Seed registra ou recupera chaves Pix antes de publicar
- **WHEN** o serviço inicializa com publicação de Pix habilitada e não há chaves Pix conhecidas,
- **THEN** ele registra ou recupera chaves Pix válidas para as contas já semeadas e armazena-as para uso na geração de transações Pix.

#### Scenario: Geração de Pix reutiliza chaves cadastradas
- **WHEN** uma transação Pix é gerada pelo produtor,
- **THEN** as chaves de origem e destino são escolhidas a partir do conjunto de chaves Pix previamente cadastradas e associadas às contas conhecidas, sem gerar valores aleatórios ou não cadastrados.

#### Scenario: Publicação bloqueada sem chaves Pix válidas
- **WHEN** o serviço não consegue garantir a existência de chaves Pix válidas (falha ao registrar/recuperar),
- **THEN** ele não publica transações Pix e registra o erro até que chaves válidas estejam disponíveis.

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


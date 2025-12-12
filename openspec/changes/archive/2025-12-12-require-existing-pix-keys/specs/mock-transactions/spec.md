## ADDED Requirements

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

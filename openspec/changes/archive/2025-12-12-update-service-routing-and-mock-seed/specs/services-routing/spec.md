## ADDED Requirements

### Requirement: Serviços consomem apenas mensagens do próprio tipo
Os consumidores `Services.Pix`, `Services.Money` e `Services.Card` SHALL aceitar/processar somente mensagens compatíveis com seu tipo de transação, descartando ou rejeitando demais tipos sem impactar processamento válido.

#### Scenario: Pix ignora mensagens não-Pix
- **WHEN** `Services.Pix` recebe uma mensagem com routing key ou tipo diferente de Pix,
- **THEN** a mensagem é rejeitada/descartada de forma segura (sem persistir ou emitir eventos) e o fato é registrado em log/métrica.

#### Scenario: Money ignora mensagens não-Money
- **WHEN** `Services.Money` recebe uma mensagem com routing key ou tipo diferente de Money,
- **THEN** a mensagem é rejeitada/descartada de forma segura e o fato é registrado em log/métrica.

#### Scenario: Card ignora mensagens não-Card
- **WHEN** `Services.Card` recebe uma mensagem com routing key ou tipo diferente de Card,
- **THEN** a mensagem é rejeitada/descartada de forma segura e o fato é registrado em log/métrica.

#### Scenario: Mensagens corretas continuam sendo processadas
- **WHEN** cada serviço recebe uma mensagem do tipo correspondente (Pix/Money/Card),
- **THEN** o processamento segue normalmente até o fim (incluindo persistência e emissões previstas).

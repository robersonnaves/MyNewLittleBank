## ADDED Requirements
### Requirement: Mensageria Rebus possui testes end-to-end com RabbitMQ real
Os testes de integração SHALL exercitar publicação e consumo via Rebus com RabbitMQ real (Testcontainers), cobrindo cenários de sucesso e falha para os serviços de mensageria.

#### Scenario: Publicação e consumo bem-sucedidos
- **WHEN** uma mensagem é publicada via abstração de mensageria com routing key de cada serviço (Pix/Money/Card/Heartbeats/Mock.Transactions),
- **THEN** ela é consumida pelo handler correspondente via Rebus e o teste verifica o processamento esperado.

#### Scenario: Falhas enviam mensagens para fila de erro após retries
- **WHEN** o handler falha além do número máximo de tentativas configurado,
- **THEN** a mensagem é encaminhada para a fila de erro/DLQ e o teste confirma a presença do item na fila ou evento equivalente.

#### Scenario: Tracing é propagado no pipeline de mensageria
- **WHEN** uma mensagem é publicada com contexto de tracing ativo,
- **THEN** os handlers recebem os cabeçalhos/identificadores de tracing permitindo correlação nos testes.

#### Scenario: Fluxo Outbox persiste e despacha para Rebus usando recursos isolados de teste
- **WHEN** uma mensagem é gravada no outbox de teste (Postgres/Testcontainers) e o dispatcher é executado,
- **THEN** a mensagem é publicada pelo Rebus nas filas/rotas de teste e consumida pelo handler correspondente, mantendo isolamento de nomes de exchange/queue específicos para a suíte.

## ADDED Requirements
### Requirement: Mensageria usa Rebus com transporte configurável
A infraestrutura de mensageria SHALL ser baseada em Rebus, permitindo escolher o transporte (RabbitMQ inicialmente) apenas por configuração, sem mudanças de código nos serviços.

#### Scenario: RabbitMQ configurado como transporte padrão
- **WHEN** a aplicação sobe com as opções de RabbitMQ preenchidas,
- **THEN** o bus Rebus conecta usando essas credenciais/topologia e publica/consome mensagens pelas filas/queues definidas.

#### Scenario: Troca de transporte sem alterar serviços
- **WHEN** o transporte Rebus é alterado por configuração para outro broker suportado,
- **THEN** os serviços continuam publicando/consumindo através da mesma abstração sem dependências diretas do broker.

### Requirement: Serviços publicam e consomem via abstração independente de broker
Os serviços SHALL usar uma camada de mensageria interna (interfaces/handlers) que não expõe tipos de Rebus ou do broker, mantendo contratos de payload e roteamento atuais.

#### Scenario: Publicação usa abstração unificada
- **WHEN** um serviço publica uma mensagem (incluindo via Outbox),
- **THEN** ele chama a interface interna de mensageria e o Rebus entrega a mensagem usando o transporte configurado, sem referências a RabbitMQ.Client ou APIs do Rebus no código do serviço.

#### Scenario: Consumo usa handlers desacoplados
- **WHEN** um serviço recebe mensagens de seu tipo (Pix/Money/Card/Heartbeats/Mock.Transactions),
- **THEN** o processamento ocorre em handlers Rebus que aplicam a filtragem de roteamento/tipo já existente, sem lidar com canais ou propriedades específicos de RabbitMQ.

### Requirement: Resiliência e observabilidade padronizadas na mensageria
A camada de mensageria SHALL oferecer configuração única para retries, DLQ/error queue e telemetria, preservando rastreabilidade e tratamento de falhas.

#### Scenario: Falha após N tentativas vai para fila de erro
- **WHEN** o processamento falha além do limite configurado de tentativas,
- **THEN** o Rebus move a mensagem para a fila de erro/DLQ configurada e registra o fato em logs/telemetria.

#### Scenario: Contexto de tracing propagado
- **WHEN** uma mensagem é publicada e consumida,
- **THEN** os identificadores de tracing são propagados pela pipeline de mensageria, permitindo correlação de logs e métricas ponta a ponta.

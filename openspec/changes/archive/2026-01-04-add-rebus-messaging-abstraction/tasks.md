## 1. Implementação
- [x] 1.1 Adicionar dependências Rebus e registrar configuração centralizada de transporte RabbitMQ (fila/error/delay) via DI, mantendo opções em `appsettings`.
- [x] 1.2 Criar abstração interna (ex.: `IMessagingBus`/`IMessageHandler`) que exponha publicação e consumo sem tipos de RabbitMQ ou Rebus nos serviços.
- [x] 1.3 Migrar publicação de mensagens (incluindo `IMessagePublisher` e `OutboxDispatcher`) para usar a abstração Rebus, preservando roteamento, retries e DLQ.
- [x] 1.4 Migrar consumidores atuais (Pix, Money, Card, Heartbeats, Mock.Transactions) para handlers Rebus, garantindo filtragem por tipo/routing existente.
- [x] 1.5 Consolidar observabilidade e health checks de mensageria usando a nova camada (telemetria/logs/checagem de conectividade).
- [x] 1.6 Atualizar/deletar componentes RabbitMQ legados e ajustar configurações/documentação.

## 2. Validação
- [x] 2.1 Executar testes de integração com Testcontainers para RabbitMQ cobrindo publicação/consumo via Rebus.
- [x] 2.2 Validar cenários de falha (retries, DLQ/error queue) e propagação de tracing.
- [x] 2.3 Rodar `openspec validate add-rebus-messaging-abstraction --strict` após os ajustes.

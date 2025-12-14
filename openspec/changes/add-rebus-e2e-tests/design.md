## Context
A camada de mensageria foi migrada para Rebus com transporte RabbitMQ, mas os testes atuais cobrem apenas Outbox/Inbox com publishers simulados. Falta uma suíte end-to-end que use RabbitMQ real e a pipeline Rebus (publish, topics, handlers, retries/DLQ e tracing) para garantir integridade do comportamento.

## Goals / Non-Goals
- Goals: criar testes de integração end-to-end com RabbitMQ real para publicação/consumo Rebus; validar roteamento por tipo; cobrir falhas e envio para error/DLQ; checar propagação mínima de tracing/telemetria; exercitar fluxo Outbox→Rebus com Postgres real para confirmar persistência/dispatch.
- Non-Goals: testar múltiplos brokers além de RabbitMQ; reescrever lógica de domínio; alterar contratos de mensagens.

## Decisions
- Usar Testcontainers RabbitMQ para ambiente isolado de testes e configurar Rebus exatamente como em produção (queues/exchanges/error queue), porém com nomes isolados de fila/rota específicos para a suíte de teste para evitar interferência.
- Exercitar handlers reais (Pix/Money/Card/Heartbeats/Mock.Transactions) via abstração de mensageria, evitando qualquer dependência direta de RabbitMQ.Client.
- Simular falhas controladas nos handlers para validar retries e envio à fila de erro.
- Validar propagação de tracing via headers/contexto com asserts mínimos (ex.: presence de traceparent) sem acoplar a implementações específicas de OpenTelemetry.
- Incluir Postgres via Testcontainers e executar fluxo Outbox→Rebus (persistir mensagem no outbox e despachar) usando os nomes de filas/rotas de teste.

## Risks / Trade-offs
- Tempo de subida do container RabbitMQ e Postgres pode aumentar duração dos testes; mitigação: reaproveitar containers por fixture.
- Flutuação de timings para retries/delay; mitigação: usar limites e timeouts determinísticos nos testes.

## Open Questions
- Algum nome de exchange/queue de teste deve seguir convenção específica para integrações externas ou podemos usar nomes isolados (ex.: `test.svc.transactions`, `test.queue.pix` etc.)?

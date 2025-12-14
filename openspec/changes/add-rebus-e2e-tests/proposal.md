# Change: Adicionar testes de integração end-to-end para Rebus

## Why
A adoção do Rebus criou uma camada de mensageria abstrata, mas ainda não há testes end-to-end que validem publicação, consumo, retries/DLQ e propagação de tracing com RabbitMQ real. Precisamos de cobertura automatizada para garantir que o comportamento previsto pelo spec de mensageria continue íntegro em cenários reais.

## What Changes
- Criar suíte de testes de integração end-to-end usando RabbitMQ real (Testcontainers) cobrindo publicação/consumo via Rebus.
- Validar comportamento de falhas: retries respeitando limites e envio para fila de erro/DLQ.
- Verificar propagação de tracing/telemetria no pipeline de mensageria.
- Incluir cenários representativos dos serviços (Pix, Money, Card, Heartbeats, Mock.Transactions) sem acoplar a APIs de broker.
- Incluir Postgres real (Testcontainers) e fluxo Outbox→Rebus para validar persistência + despacho, usando nomes de filas/rotas específicos para testes isolados.

## Impact
- Affected specs: messaging-bus
- Affected code: testes de integração (pipelines Rebus + RabbitMQ + Postgres/outbox), fixtures/utilitários de mensageria e configuração de testes.

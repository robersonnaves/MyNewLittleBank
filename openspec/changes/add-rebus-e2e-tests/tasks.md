## 1. Implementação
- [x] 1.1 Definir escopo e dados de teste cobrindo publish/consume para Pix/Money/Card/Heartbeats/Mock.Transactions via Rebus com nomes de filas/rotas isolados de teste.
- [x] 1.2 Montar harness de integração com RabbitMQ real (Testcontainers) configurando Rebus e dependências necessárias, incluindo error/DLQ de teste.
- [x] 1.3 Incluir Postgres (Testcontainers) para validar fluxo Outbox→Rebus: persistir, despachar e consumir mensagens nos nomes de fila de teste.
- [x] 1.4 Implementar testes de sucesso (publica e consome) garantindo roteamento correto e entrega aos handlers.
- [x] 1.5 Implementar testes de falha cobrindo retries e envio para fila de erro/DLQ após exceder limite.
- [x] 1.6 Validar propagação de tracing/telemetria no pipeline de mensageria.
- [x] 1.7 Documentar/limpar fixtures e alinhar configurações de testes com o spec.

## 2. Validação
- [ ] 2.1 Executar suíte de integração Rebus end-to-end localmente (incluindo Testcontainers).
- [x] 2.2 Rodar `openspec validate add-rebus-e2e-tests --strict`.

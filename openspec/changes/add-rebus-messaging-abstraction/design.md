## Context
A mensageria hoje usa `RabbitMQ.Client` diretamente via serviços internos (`PublisherService`, `RabbitConsumerService`, `RabbitTopologyBootstrapper`, `IRabbitConnectionFactory`). Os serviços Pix/Money/Card/Heartbeats/Mock.Transactions referenciam explicitamente tipos de RabbitMQ e dependem de opções específicas (exchange, queues, DLQ/delay) configuradas manualmente. O Outbox despacha mensagens usando o publisher nativo, o que dificulta a troca do broker e multiplica pontos de configuração/observabilidade.

## Goals / Non-Goals
- Goals: adotar Rebus como barramento para publicação/consumo; ocultar APIs de broker e de Rebus atrás de uma abstração interna; manter RabbitMQ como transporte inicial com retries/DLQ equivalentes; reutilizar outbox/inbox e tracing existentes sem duplicar lógica.
- Non-Goals: introduzir suporte a múltiplos brokers simultaneamente; alterar contratos de payload ou roteamento funcional; reescrever lógica de domínio/transação além da integração de mensageria.

## Decisions
- Decision: usar Rebus (com `Rebus.RabbitMq`) como transporte padrão configurado via DI único, permitindo trocar transporte via configuração futura sem tocar nos serviços.
  - Alternatives considered: manter implementação nativa RabbitMQ.Client (continua acoplamento e aumenta manutenção) ou adotar outros frameworks como MassTransit (banido no projeto).
- Decision: expor uma abstração interna (e.g., `IMessagingBus` e handlers) para publicação/consumo, evitando que serviços conheçam Rebus ou RabbitMQ; a DI fornecerá wrappers/adapters.
- Decision: integrar Outbox/Inbox com Rebus para que o despacho e consumo continuem idempotentes; filas de erro e retries usarão mecanismos nativos do Rebus mapeados para as filas já existentes (ou equivalentes) do RabbitMQ.
- Decision: preservar observabilidade (logs/OTLP e correlation) usando middlewares Rebus, substituindo health check específico de Rabbit por um check de conectividade do bus/configuração do transporte.

## Risks / Trade-offs
- Adoção de nova dependência aumenta superfície de configuração; mitigação: encapsular em um módulo único de mensageria e documentar opções.
- Divergência de semântica de retries/DLQ entre Rebus e lógica atual; mitigação: alinhar configurações de número de tentativas/filas e validar com testes de falha.
- Possível impacto em testes de integração existentes que assumem classes Rabbit; mitigação: atualizar fábricas de serviço e casos de teste para usar o bus abstrato.

## Migration Plan
1) Introduzir infraestrutura Rebus (packages, DI, opções) com RabbitMQ como transporte default e filas equivalentes às atuais.
2) Criar adaptação de publicação/consumo e migrar Outbox/Inbox para a nova abstração.
3) Converter consumidores dos serviços para handlers Rebus e remover dependências diretas de RabbitMQ.Client/topologia customizada.
4) Substituir health checks e ajustes de observabilidade pelo pipeline Rebus.
5) Remover classes obsoletas e validar tudo com Testcontainers (RabbitMQ) e cenários de erro/retry/DLQ.

## Open Questions
- Devemos preservar exatamente os nomes de exchange/queues atuais ou podemos usar convenções Rebus (impacta compatibilidade com integrações externas)?
- Precisamos de atraso customizado por exchange/queue ou podemos usar o mecanismo de deferred messages do Rebus?
- Há necessidade de expor métricas específicas de broker além das providas pelo Rebus (para Prometheus/OTLP)?

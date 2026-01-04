# Change: Adicionar camada de mensageria baseada em Rebus

## Why
Hoje cada serviço depende diretamente de RabbitMQ.Client e de um conjunto de classes de infraestrutura próprias, o que acopla o código ao broker atual e dificulta trocar o transporte no futuro. Precisamos de uma camada de mensageria padronizada, baseada em Rebus, que mantenha o RabbitMQ como transporte imediato, mas esconda detalhes do broker e permita evoluir para outros provedores sem reescrever as integrações dos serviços.

## What Changes
- Introduzir Rebus como barramento de mensagens padrão, configurado inicialmente com transporte RabbitMQ.
- Criar abstração interna para publicar e consumir mensagens sem expor APIs específicas de broker ou do Rebus nos serviços.
- Migrar publicação (incluindo Outbox) e consumo atuais para a nova camada, preservando semântica de roteamento, retries e DLQ.
- Consolidar configuração e observabilidade (telemetria/logs/health) de mensageria em um único ponto.

## Impact
- Affected specs: messaging-bus (nova)
- Affected code: Infra.Message, Domain.Interfaces (contratos de mensageria), serviços de consumo/publicação (Pix, Money, Card, Heartbeats, Mock.Transactions), Outbox/Inbox e health checks ligados a RabbitMQ.

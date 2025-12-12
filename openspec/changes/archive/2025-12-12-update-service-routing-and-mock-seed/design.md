# Design: Roteamento por serviço e seed expandido do Mock.Transactions

## Contexto
- Serviços `Services.Pix`, `Services.Money`, `Services.Card` consomem mensagens via RabbitMQ; atualmente não há garantia explícita de filtragem por tipo.
- `Mock.Transactions` semeia entidades via API antes de publicar, mas com quantidade limitada de clientes e contas determinísticas.

## Decisões
- **Filtro por tipo**: Cada serviço consumirá apenas mensagens cujo tipo/routing key corresponda ao domínio (ex.: `mock.PIX` ou routing key `pix.transactions` para Pix). Mensagens divergentes serão descartadas ou negativadas sem afetar métricas de sucesso.
- **Seed ampliado**: `Mock.Transactions` criará exatamente 10 clientes e, para cada cliente, um número aleatório de contas entre 1 e 3, garantindo pelo menos uma conta por cliente.
- **Config valida**: Ajustar validações para os novos limites (clientes=10, contas mín.=1, máx.=3) e refletir isso em appsettings/exemplos.

## Riscos / Trade-offs
- Se o broker enviar mensagens com routing keys inesperadas, haverá descarte intencional; precisamos garantir logs/metricas para depuração.
- A aleatoriedade de contas pode gerar variações em testes; documentar faixa e, se necessário, permitir seed fixo para determinismo.

## Próximos Passos de Implementação
- Revisar consumidores em cada serviço para localizar onde checar o tipo (headers, routing key, payload).
- Introduzir validação/configuração de seed no Mock.Transactions para novos limites e comportamento aleatório controlado (ex.: seed opcional).

# Tasks: Restringir processamento por serviço e ajustar seed do Mock.Transactions

## 1. Roteamento por tipo de mensagem
- [x] 1.1 Mapear como cada serviço identifica tipo de transação (routing key, message type) e documentar filtros necessários.
- [x] 1.2 Implementar filtro de consumo no `Services.Pix` para aceitar apenas mensagens Pix; registrar comportamento para mensagens não correspondentes.
- [x] 1.3 Implementar filtro de consumo no `Services.Money` para aceitar apenas mensagens Money; registrar comportamento para mensagens não correspondentes.
- [x] 1.4 Implementar filtro de consumo no `Services.Card` para aceitar apenas mensagens Card; registrar comportamento para mensagens não correspondentes.
- [x] 1.5 Adicionar testes (ou smoke) que garantam rejeição/ignorância segura para tipos incorretos.

## 2. Seed do Mock.Transactions
- [x] 2.1 Ajustar configuração do seed para criar 10 clientes.
- [x] 2.2 Definir geração aleatória de 1 a 3 contas por cliente e validar intervalo.
- [x] 2.3 Atualizar validações/configuração para refletir novos limites (seed e roteamento).
- [x] 2.4 Validar manualmente (ou via teste) que o seed cria 10 clientes e cada cliente tem 1-3 contas antes de publicar transações.

## 3. Observabilidade e documentação
- [x] 3.1 Registrar logs/metricas de mensagens ignoradas por tipo e do seed ajustado.
- [x] 3.2 Atualizar documentação/configuração (appsettings/examples) conforme novos parâmetros.

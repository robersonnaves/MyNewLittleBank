# Tasks: Mock.Transactions gera dados apenas para clientes/contas registradas

## 1. Seed de entidades via API
- [x] 1.1 Mapear endpoints e payloads da API para criar clientes e contas; definir variáveis de ambiente (URL/autenticação) necessárias ao mock.
- [x] 1.2 Implementar rotina de seed no `Mock.Transactions` que cria (ou recupera) clientes e contas antes de publicar transações; registrar status e erros.
- [x] 1.3 Evitar duplicação: reutilizar cadastros existentes quando encontrados (ex.: por identificadores fixos ou busca na API).

## 2. Geração de transações vinculadas
- [x] 2.1 Ajustar o gerador para usar somente IDs de clientes/contas já registrados, garantindo coerência entre transações e dados base.
- [x] 2.2 Configurar parâmetros (ex.: quantidade de clientes/contas, opção de desabilitar seed) e validar que a emissão de transações não ocorre se o seed falhar.

## 3. Validação
- [ ] 3.1 Executar o `Mock.Transactions` contra a API local e verificar que as transações publicadas referenciam apenas entidades existentes.

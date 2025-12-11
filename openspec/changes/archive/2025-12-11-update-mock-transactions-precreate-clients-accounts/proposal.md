# Change: Mock.Transactions gera dados apenas para clientes/contas registradas

## Why
As transações de mock hoje podem referenciar clientes e contas inexistentes, gerando inconsistências e falhas de processamento. Precisamos garantir que o produtor de transações trabalhe apenas com entidades já registradas, criando-as previamente via API quando necessário.

## What Changes
- Incluir um fluxo de pré-criação que chama a `src/API/API.csproj` para registrar clientes e contas antes de publicar qualquer transação de mock.
- Garantir que o gerador utilize somente IDs de clientes e contas já registrados (persistidos ou recuperados da API) ao montar transações.
- Expor configurações para URL da API, quantidade de clientes/contas a criar, e permitir desabilitar ou reutilizar cadastros existentes.
- Tratar falhas de pré-criação de forma segura (não publicar transações sem entidades válidas) e registrar claramente o estado do seed.

## Impact
- Affected specs: mock-transactions
- Affected code: src/Mock.Transactions/Mock.Transactions.csproj, src/API/API.csproj (consumo via REST)

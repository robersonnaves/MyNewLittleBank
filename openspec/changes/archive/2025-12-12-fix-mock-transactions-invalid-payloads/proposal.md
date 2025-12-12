# Change: Impedir Mock.Transactions de publicar payloads inválidos

## Why
- O ERROS_SERVICES_ANALISE.md mostra que os serviços card, pix e money recebem transações inválidas (transaction_id_empty, pix_keys_invalid, card_number_empty), originadas pelo produtor Mock.Transactions.
- Precisamos corrigir o produtor para não publicar mensagens que violem validações de domínio, evitando reentrega, DLQ e ruído operacional.

## What Changes
- Validar e bloquear no Mock.Transactions qualquer payload com TransactionId vazio/ausente.
- Garantir que geradores de Pix usem chaves não vazias e cadastradas; geradores de cartão emitam números não vazios/válidos antes de publicar.
- Registrar erro e não publicar quando os dados mínimos estiverem inválidos, mantendo visibilidade para troubleshooting.

## Impact
- Affected specs: mock-transactions
- Affected code: src/Mock.Transactions/Mock.Transactions.csproj e geradores/fluxo de publicação

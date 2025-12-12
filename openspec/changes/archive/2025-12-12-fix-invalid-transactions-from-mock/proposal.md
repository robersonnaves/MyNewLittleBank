# Change: Garantir transações mockadas sempre válidas

## Why
- O ERROS_SERVICES_ANALISE.md mostra mensagens inválidas chegando aos serviços Card/Pix/Money (`transaction_id_empty`, `pix_keys_invalid`, `card_number_empty`) e indo para DLQ após tentativas.
- Em vez de bloquear mensagens inválidas, precisamos fazer o Mock.Transactions gerar apenas payloads completos e válidos, eliminando a origem do problema.

## What Changes
- Ajustar o Mock.Transactions para garantir TransactionId não vazio, chaves Pix válidas e número de cartão presente/formatado antes de publicar.
- Garantir que geradores (Pix/Money/Card) produzam dados consistentes e alinhados às regras de domínio, evitando reentregas e DLQ por invalidez.
- Cobrir com testes e observabilidade para detectar rapidamente qualquer regressão na geração.

## Impact
- Affected specs: mock-transactions
- Affected code: src/Mock.Transactions/*

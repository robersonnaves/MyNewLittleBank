# Change: Expandir chaves Pix mockadas e garantir persistência de saldo

## Why
- As transações mockadas precisam refletir o mundo real: chaves Pix podem ser telefone, email, CPF ou chave aleatória (GUID), e devem ser tratadas pelo produtor.
- O saldo em `bank_accounts` não está sendo atualizado no banco mesmo com transações processadas, indicando que a persistência do saldo precisa ser garantida.

## What Changes
- Atualizar o Mock.Transactions para gerar e usar chaves Pix válidas de tipos variados (telefone, email, CPF, chave aleatória) associadas às contas mockadas.
- Garantir que o processamento de transações atualize e persista o saldo da conta na tabela `bank_accounts` após débitos/créditos.
- Cobrir os fluxos com validação (logs/testes) para evitar regressões e detectar inconsistências.

## Impact
- Affected specs: mock-transactions, account-services (persistência de saldo)
- Affected code: src/Mock.Transactions/*, src/UseCases/Transactions/*, src/Infra.Database/*

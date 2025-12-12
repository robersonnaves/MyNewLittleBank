# Change: Exigir chaves Pix cadastradas para transações Pix

## Why
- O Mock.Transactions hoje gera chaves Pix aleatórias que não estão cadastradas, podendo gerar rejeições ou inconsistências ao processar.
- Precisamos alinhar o fluxo Pix ao comportamento de transações em dinheiro, em que as contas usadas já estão previamente criadas.

## What Changes
- Fazer o seed do Mock.Transactions resolver ou cadastrar chaves Pix válidas associadas às contas antes de publicar transações Pix.
- Garantir que a geração de transações Pix use exclusivamente chaves Pix já cadastradas; bloquear publicação quando não houver chaves válidas disponíveis.
- Ajustar validações/configuração para refletir a dependência de chaves Pix pré-existentes.

## Impact
- Affected specs: mock-transactions
- Affected code: src/Mock.Transactions/*, possivelmente src/Services.Pix/*

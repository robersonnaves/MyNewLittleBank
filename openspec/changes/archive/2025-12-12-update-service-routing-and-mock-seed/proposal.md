# Change: Restringir processamento por serviço e ajustar seed do Mock.Transactions

## Why
- Hoje os serviços de consumo podem processar mensagens fora de seu domínio (pix, money, card), abrindo brechas para erros e métricas incorretas.
- O seed do Mock.Transactions precisa gerar mais clientes e variar contas por cliente para testes mais realistas e volumosos.

## What Changes
- Garantir que cada serviço de consumo (`Services.Pix`, `Services.Money`, `Services.Card`) processe apenas mensagens do tipo correspondente, ignorando/rejeitando demais tipos.
- Atualizar o Mock.Transactions para semear 10 clientes, com 1 a 3 contas por cliente escolhidas aleatoriamente, antes de publicar transações.
- Ajustar configuração/validação para refletir os novos limites de seed e roteamento esperado.

## Impact
- Affected specs: services-routing, mock-transactions
- Affected code: src/Services.Pix/*, src/Services.Money/*, src/Services.Card/*, src/Mock.Transactions/*

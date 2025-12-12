# Change: Incluir Mock.Transactions no start-all.sh

## Why
O script `infra/scripts/start-all.sh` hoje apenas sobe os stacks de infraestrutura e aplicação via Docker Compose, mas não inicia o projeto `src/Mock.Transactions/Mock.Transactions.csproj`. Isso deixa de gerar tráfego simulado e exige passos manuais para ligar o produtor de transações durante o desenvolvimento.

## What Changes
- Ajustar o `start-all.sh` para também iniciar o worker `Mock.Transactions` (executando o `Mock.Transactions.csproj` ou seu contêiner correspondente).
- Garantir que a execução do mock fique alinhada ao ciclo de vida do script (logs e encerramento limpo).
- Documentar o uso e quaisquer flags/variáveis para habilitar ou desabilitar o mock quando necessário.

## Impact
- Affected specs: devops-scripts
- Affected code: infra/scripts/start-all.sh, src/Mock.Transactions/*

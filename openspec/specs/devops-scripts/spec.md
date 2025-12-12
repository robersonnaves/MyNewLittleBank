# devops-scripts Specification

## Purpose
TBD - created by archiving change add-mock-transactions-start-all. Update Purpose after archive.
## Requirements
### Requirement: start-all inicia Mock.Transactions
O script `infra/scripts/start-all.sh` SHALL iniciar também o produtor de transações de mock (`src/Mock.Transactions/Mock.Transactions.csproj`), mantendo-o alinhado ao ciclo de vida do script.

#### Scenario: Mock.Transactions sobe junto com a stack
- **WHEN** o `start-all.sh` é executado com a configuração padrão,
- **THEN** além dos serviços de infraestrutura e aplicação, o `Mock.Transactions` é iniciado automaticamente (via `dotnet run` ou contêiner equivalente) com as variáveis necessárias.

#### Scenario: Execução pode ser desabilitada por flag
- **WHEN** o script é executado com uma flag/variável que desabilita o mock,
- **THEN** o `Mock.Transactions` não é iniciado e os demais serviços sobem normalmente.

#### Scenario: Encerramento limpo
- **WHEN** o `start-all.sh` é interrompido ou finalizado,
- **THEN** o processo do `Mock.Transactions` também é encerrado/limpo para evitar instâncias órfãs.


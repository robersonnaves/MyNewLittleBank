# Tasks: Incluir Mock.Transactions no start-all.sh

## 1. Planejamento e ajustes no script
- [x] 1.1 Mapear o comportamento atual do `start-all.sh` e as dependências do `Mock.Transactions` (RabbitMQ, vars).
- [x] 1.2 Definir como iniciar o `Mock.Transactions` a partir do script (ex.: `dotnet run --project src/Mock.Transactions/Mock.Transactions.csproj` ou equivalente em contêiner), incluindo modo em background e captura mínima de logs.
- [x] 1.3 Ajustar o `start-all.sh` para iniciar/parar o `Mock.Transactions` junto com os demais serviços, preservando idempotência e limpeza.
- [x] 1.4 Documentar flags/variáveis para habilitar/desabilitar o mock e atualizar README/scripts de uso se aplicável.
- [ ] 1.5 Validar manualmente executando o script e verificando que o `Mock.Transactions` está rodando e publicando mensagens (pendente: validação manual não executada neste ambiente).

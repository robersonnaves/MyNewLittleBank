## Infraestrutura e testes

### Compose (Docker/Podman)
- Arquivo: `infra/docker-compose.yml` com perfis `local` e `ci`.
- Serviços: Postgres, RabbitMQ, OpenSearch, Jaeger, Prometheus.
- Variáveis: use `.env` baseado em `infra/.env.example`.
- Subir: `CONTAINER_ENGINE=podman ./infra/scripts/bootstrap-compose.sh local` (ou `ci` para rodar com limpeza). Se não definir, usa `docker` por padrão.

### Testes de integração
- Projeto: `tests/Integration/MyNewLittleBank.Tests.Integration.csproj`.
- Execução direta: `dotnet test --filter "Category=Integration"`.
- Script conveniente (sobe compose perfil `ci`): `CONTAINER_ENGINE=podman ./infra/scripts/run-integration-tests.sh`.
- Fixtures usam Testcontainers para Postgres/RabbitMQ; OutboxDispatcher e InboxMessageStore cobertos por testes.
  - Para Podman, defina `CONTAINER_ENGINE=podman` e, se necessário, `DOCKER_HOST=unix:///run/user/$UID/podman/podman.sock`.

### CI (GitHub Actions)
- Workflow: `.github/workflows/ci.yml` com jobs `build`, `unit-tests`, `integration-tests` (compose + dotnet test) e `docker-build` (imagens dos serviços).
